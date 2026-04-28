using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Infrastructure.Persistence;

namespace OIO.Infrastructure.Scheduling.Jobs.Orders;

public sealed class CancelExpiredOrdersJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CancelExpiredOrdersJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5); // Run every 5 minutes

    public CancelExpiredOrdersJob(
        IServiceScopeFactory scopeFactory,
        ILogger<CancelExpiredOrdersJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Cancel expired orders job started. Interval: {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredOrdersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cancelling expired orders.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task CleanupExpiredOrdersAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var runtimeSettings = scope.ServiceProvider.GetRequiredService<IRuntimeSettings>();

        var nowUtc = clock.UtcNow;

        // 1. Fetch orders that are PendingPayment and over expiration limit
        // Using `ToList` inside memory for aggregate operations because we need to utilize Domain Events/Behavior
        var expiredOrders = await dbContext.Set<Order>()
            .Where(o => o.Status == OrderStatus.PendingPayment && o.PaymentDueAt != null && o.PaymentDueAt < nowUtc)
            .Take(100) // process in batches
            .ToListAsync(ct);

        if (expiredOrders.Count == 0) return;

        int cancelledCount = 0;
        foreach (var order in expiredOrders)
        {
            var result = order.Cancel("Payment deadline expired", nowUtc);
            if (result.IsSuccess)
            {
                await MarkAuctionPaymentDefaultedAsync(dbContext, runtimeSettings, order, nowUtc, ct);
                cancelledCount++;
            }
            else
            {
                _logger.LogWarning("Could not cancel expired order {OrderId}: {Error}", order.Id.Value, result.Error.Message);
            }
        }

        if (cancelledCount > 0)
        {
            await dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Cancelled {CancelledCount} expired orders out of {TotalCount} found.", cancelledCount, expiredOrders.Count);
        }
    }

    private async Task MarkAuctionPaymentDefaultedAsync(
        ApplicationDbContext dbContext,
        IRuntimeSettings runtimeSettings,
        Order order,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var auction = await dbContext.Set<Auction>()
            .Include(x => x.Item)
            .Include(x => x.WinnerOffers)
            .Include(x => x.Bids)
            .Include(x => x.Deposits)
            .FirstOrDefaultAsync(x => x.Id == order.AuctionId, cancellationToken);

        if (auction is null)
            return;

        if (auction.WinnerId != order.BuyerId)
        {
            _logger.LogWarning(
                "CancelExpiredOrdersJob: skip marking payment defaulted for auction {AuctionId} because order buyer {BuyerId} is not the current winner {WinnerId}.",
                auction.Id.Value,
                order.BuyerId.Value,
                auction.WinnerId?.Value);
            return;
        }

        var paymentDefaultResult = auction.MarkPaymentDefaulted(nowUtc);
        if (paymentDefaultResult.IsFailure)
        {
            _logger.LogWarning(
                "Unable to mark auction {AuctionId} as payment_defaulted after expired order {OrderId}: {Error}",
                auction.Id.Value,
                order.Id.Value,
                paymentDefaultResult.Error.Message);
            return;
        }

        var riskFlag = UserRiskFlag.Create(
            userId: order.BuyerId,
            flagType: "non_payment",
            reason: $"Order {order.OrderNumber.Value} expired without payment.",
            severity: RiskFlagSeverity.Medium,
            createdBy: null,
            nowUtc: nowUtc);

        dbContext.Set<UserRiskFlag>().Add(riskFlag);

        var alert = MonitoringAlert.Create(
            entityType: "User",
            entityId: order.BuyerId.Value,
            alertType: "repeated_non_payment",
            severity: AlertSeverity.Medium,
            payload: System.Text.Json.JsonSerializer.Serialize(new
            {
                userId = order.BuyerId.Value,
                orderId = order.Id.Value,
                auctionId = auction.Id.Value
            }),
            nowUtc: nowUtc);

        dbContext.Set<MonitoringAlert>().Add(alert);

        var suspendEnabled = runtimeSettings.Ops.AutoSuspendAfterNonPaymentCount;

        if (suspendEnabled > 0)
        {
            var priorFlags = await dbContext.Set<UserRiskFlag>()
                .CountAsync(
                    x => x.UserId == order.BuyerId && x.FlagType == "non_payment",
                    cancellationToken);

            if (priorFlags + 1 >= suspendEnabled)
            {
                var user = await dbContext.Set<User>()
                    .FirstOrDefaultAsync(x => x.Id == order.BuyerId, cancellationToken);

                if (user is not null && user.Status == UserStatus.Active)
                {
                    var suspendResult = user.ChangeStatus(UserStatus.Suspended, nowUtc);
                    if (suspendResult.IsFailure)
                    {
                        _logger.LogWarning(
                            "Failed to auto suspend user {UserId} after repeated non-payment: {Error}",
                            user.Id.Value,
                            suspendResult.Error.Message);
                    }
                }
            }
        }
    }
}
