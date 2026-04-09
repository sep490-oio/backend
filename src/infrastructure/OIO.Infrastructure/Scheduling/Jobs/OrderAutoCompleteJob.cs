using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.OrderContext.Services;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Aggregates.SellerDirectShipments;
using OIO.Domain.Context.OrderContext.Enums;
using Quartz;

namespace OIO.Infrastructure.Scheduling.Jobs;

/// <summary>
/// Canonical auto-complete job for all fulfillment types. Picks up every
/// <see cref="OrderStatus.Delivered"/> order whose buyer decision window has
/// elapsed — regardless of whether fulfillment was <c>SellerDirectShipment</c>
/// or warehouse-managed <c>OutboundShipment</c>. Delegates the actual
/// transition to <see cref="IOrderReceiptService.ConfirmAsync"/> with
/// <c>systemInvoked: true</c> so the same code path runs as a buyer
/// "Confirm receipt" click.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class OrderAutoCompleteJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClock _clock;
    private readonly IOptionsMonitor<AppLoggingOptions> _loggingOptions;
    private readonly ILogger<OrderAutoCompleteJob> _logger;

    public OrderAutoCompleteJob(
        IServiceScopeFactory scopeFactory,
        IClock clock,
        IOptionsMonitor<AppLoggingOptions> loggingOptions,
        ILogger<OrderAutoCompleteJob> logger)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
        _loggingOptions = loggingOptions;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
        var receiptService = scope.ServiceProvider.GetRequiredService<IOrderReceiptService>();

        var ct = context.CancellationToken;
        var now = _clock.UtcNow;

        // Canonical window: DecisionWindowEndsAt is populated by MarkAsDelivered
        // from the runtime-configured ReturnDecisionWindowDays. We filter on it
        // directly rather than recomputing DeliveredAt + window so the job honors
        // the exact window the order was stamped with at delivery time.
        // Filter is fulfillment-type agnostic — seller direct and warehouse
        // outbound are both covered by Order.Status == Delivered.
        // Exclude seller direct shipments flagged for manual review — those are
        // handled by ScanOverdueSelfShipOrdersJob which raises a MonitoringAlert
        // instead of auto-completing. Warehouse outbound orders have no matching
        // row in SellerDirectShipments so the left-join keeps them eligible.
        // Terminal dispute statuses — orders with active disputes are excluded
        var terminalStatuses = new[] { "resolved", "rejected", "cancelled" };

        var eligibleOrderIds = await (
            from o in dbContext.Set<Order>()
            join s in dbContext.Set<SellerDirectShipment>() on o.Id equals s.OrderId into sj
            from s in sj.DefaultIfEmpty()
            where o.Status == OrderStatus.Delivered
                  && o.DecisionWindowEndsAt != null
                  && o.DecisionWindowEndsAt < now
                  && (s == null || !s.ManualReviewRequired)
                  && !dbContext.Set<Dispute>().Any(d =>
                      d.OrderId == o.Id
                      && !terminalStatuses.Contains(d.Status.Id))
            orderby o.DecisionWindowEndsAt
            select o.Id)
            .Take(200)
            .ToListAsync(ct);

        var completedCount = 0;
        var failedCount = 0;

        foreach (var orderId in eligibleOrderIds)
        {
            try
            {
                var result = await receiptService.ConfirmAsync(orderId.Value, systemInvoked: true, now, ct);
                if (result.IsFailure)
                {
                    failedCount++;
                    _logger.LogWarning(
                        "Auto-complete ConfirmAsync failed for Order {OrderId}: {Error}",
                        orderId.Value, result.Error.Message);
                    continue;
                }

                completedCount++;
            }
            catch (Exception ex)
            {
                failedCount++;
                _logger.LogError(ex, "Error auto-completing Order {OrderId}", orderId.Value);
            }
        }

        stopwatch.Stop();
        var logging = _loggingOptions.CurrentValue.Jobs;

        if (eligibleOrderIds.Count > 0 || failedCount > 0)
        {
            var level = failedCount > 0 || stopwatch.ElapsedMilliseconds >= logging.SlowJobThresholdMs
                ? LogLevel.Warning
                : LogLevel.Information;

            _logger.Log(
                level,
                "OrderAutoCompleteJob completed in {DurationMs}ms. Eligible={EligibleCount}, Completed={CompletedCount}, Failed={FailedCount}",
                stopwatch.ElapsedMilliseconds,
                eligibleOrderIds.Count,
                completedCount,
                failedCount);
        }
        else if (stopwatch.ElapsedMilliseconds >= logging.SlowJobThresholdMs)
        {
            _logger.LogWarning(
                "OrderAutoCompleteJob found no eligible orders in {DurationMs}ms.",
                stopwatch.ElapsedMilliseconds);
        }
        else if (logging.LogNoopRuns)
        {
            _logger.LogDebug(
                "OrderAutoCompleteJob found no eligible orders in {DurationMs}ms.",
                stopwatch.ElapsedMilliseconds);
        }
    }
}
