using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Application.Context.OrderContext.Services;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Aggregates.SellerDirectShipments;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Infrastructure.Persistence;

namespace OIO.Infrastructure.Scheduling.Jobs.Orders;

public sealed class ScanOverdueSelfShipOrdersJob : BackgroundService
{
    private const string OverdueReason = "seller_ship_sla_missed";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScanOverdueSelfShipOrdersJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1); // Run every 1 minute

    public ScanOverdueSelfShipOrdersJob(
        IServiceScopeFactory scopeFactory,
        ILogger<ScanOverdueSelfShipOrdersJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Scan overdue self-ship orders job started. Interval: {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScanOverdueSelfShipOrdersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while scanning overdue self-ship orders.");
            }

            try
            {
                await RaiseManualReviewAlertsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while raising manual-review alerts for direct shipments.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ScanOverdueSelfShipOrdersAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var nowUtc = clock.UtcNow;

        // 1. Fetch orders that are past their seller ship-by SLA and still in a pre-shipped state.
        // Order by oldest ShipByAt first so the worst-overdue rows are processed deterministically.
        var overdueOrders = await dbContext.Set<Order>()
            .Where(o => o.ShipByAt != null
                        && !o.IsShippingOverdue
                        && o.ShipByAt < nowUtc
                        && (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Processing))
            .OrderBy(o => o.ShipByAt)
            .Take(100) // process in batches
            .ToListAsync(ct);

        if (overdueOrders.Count == 0) return;

        var newlyOverdue = new List<Order>();
        foreach (var order in overdueOrders)
        {
            var result = order.MarkShippingOverdue(OverdueReason, nowUtc);
            if (result.IsFailure)
            {
                _logger.LogWarning("Could not mark overdue order {OrderId}: {Error}", order.Id.Value, result.Error.Message);
                continue;
            }

            // Only emit MonitoringAlert + notification on the actual transition
            // (newly overdue). Idempotent re-scans of already-overdue rows are no-ops.
            if (!result.Value) continue;

            var alert = MonitoringAlert.Create(
                entityType: "Order",
                entityId: order.Id.Value,
                alertType: OverdueReason,
                severity: AlertSeverity.Medium,
                payload: System.Text.Json.JsonSerializer.Serialize(new
                {
                    orderId = order.Id.Value,
                    sellerId = order.SellerId.Value,
                    buyerId = order.BuyerId.Value,
                    auctionId = order.AuctionId.Value,
                    shipByAt = order.ShipByAt
                }),
                nowUtc: nowUtc);

            dbContext.Set<MonitoringAlert>().Add(alert);
            newlyOverdue.Add(order);
        }

        if (newlyOverdue.Count > 0)
        {
            await dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Marked {MarkedCount} overdue self-ship orders out of {TotalCount} found.", newlyOverdue.Count, overdueOrders.Count);

            // Fan-out seller notifications. Admins are covered by the MonitoringAlert
            // entries persisted above.
            foreach (var order in newlyOverdue)
            {
                try
                {
                    var notifyResult = await sender.Send(new CreateNotificationCommand(
                        UserId: order.SellerId.Value,
                        NotificationType: "order",
                        EventType: "order_shipping_overdue",
                        Title: "Đơn hàng quá hạn giao",
                        Message: $"Đơn hàng {order.OrderNumber.Value} đã quá hạn giao 3 ngày kể từ khi thanh toán.",
                        Priority: NotificationPriority.High,
                        EntityType: "Order",
                        EntityId: order.Id.Value,
                        Metadata: System.Text.Json.JsonSerializer.Serialize(new
                        {
                            orderId = order.Id.Value,
                            orderNumber = order.OrderNumber.Value,
                            shipByAt = order.ShipByAt
                        })),
                        ct);

                    if (notifyResult.IsFailure)
                    {
                        _logger.LogWarning(
                            "Failed to dispatch overdue notification for order {OrderId}: {Error}",
                            order.Id.Value, notifyResult.Error.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to dispatch overdue notification for order {OrderId}.",
                        order.Id.Value);
                }
            }
        }
    }

    /// <summary>
    /// Second pass — raise <see cref="MonitoringAlert"/> entries for
    /// direct-ship orders flagged for manual review whose decision window has
    /// elapsed. Normal auto-complete (both seller-direct and warehouse
    /// outbound) is handled by the canonical
    /// <c>OrderAutoCompleteJob</c>; this pass only covers the manual-review
    /// escape hatch so flagged orders are surfaced to ops instead of being
    /// auto-settled.
    /// </summary>
    private async Task RaiseManualReviewAlertsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var nowUtc = clock.UtcNow;

        // Manual-review shipments past window → create MonitoringAlert, skip auto-complete.
        var manualReviewQuery =
            from o in dbContext.Set<Order>().AsNoTracking()
            join s in dbContext.Set<SellerDirectShipment>().AsNoTracking()
                on o.Id equals s.OrderId
            where o.Status == OrderStatus.Delivered
                  && o.DecisionWindowEndsAt != null
                  && o.DecisionWindowEndsAt < nowUtc
                  && o.Status != OrderStatus.Completed
                  && o.Status != OrderStatus.Disputed
                  && s.ManualReviewRequired
            orderby o.DecisionWindowEndsAt
            select new { OrderId = o.Id, o.SellerId, o.BuyerId, o.AuctionId, o.DecisionWindowEndsAt, s.ManualReviewReason };

        var manualReviewRows = await manualReviewQuery.Take(100).ToListAsync(ct);

        if (manualReviewRows.Count > 0)
        {
            foreach (var row in manualReviewRows)
            {
                // Idempotency: only create the alert once per order.
                var orderGuid = row.OrderId.Value;
                var alreadyAlerted = await dbContext.Set<MonitoringAlert>()
                    .AsNoTracking()
                    .AnyAsync(a => a.EntityType == "Order"
                                   && a.EntityId == orderGuid
                                   && a.AlertType == "manual_review_needed", ct);

                if (alreadyAlerted)
                {
                    _logger.LogDebug(
                        "Manual-review alert already exists for order {OrderId}; skipping auto-complete.",
                        orderGuid);
                    continue;
                }

                var alert = MonitoringAlert.Create(
                    entityType: "Order",
                    entityId: orderGuid,
                    alertType: "manual_review_needed",
                    severity: AlertSeverity.High,
                    payload: System.Text.Json.JsonSerializer.Serialize(new
                    {
                        orderId = orderGuid,
                        sellerId = row.SellerId.Value,
                        buyerId = row.BuyerId.Value,
                        auctionId = row.AuctionId.Value,
                        decisionWindowEndsAt = row.DecisionWindowEndsAt,
                        reason = row.ManualReviewReason
                    }),
                    nowUtc: nowUtc);

                dbContext.Set<MonitoringAlert>().Add(alert);
                _logger.LogInformation(
                    "Order {OrderId} past decision window but flagged for manual review — skipping auto-complete and raising alert.",
                    orderGuid);
            }

            await dbContext.SaveChangesAsync(ct);
        }
    }
}
