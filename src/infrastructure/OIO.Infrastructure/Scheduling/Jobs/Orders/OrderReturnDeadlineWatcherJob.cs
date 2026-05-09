using System.Diagnostics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.OrderContext.Commands.CreateAdminRefundRetryTicket;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Aggregates.Orders.Events;
using OIO.Domain.Context.OrderContext.Enums;
using Quartz;

namespace OIO.Infrastructure.Scheduling.Jobs.Orders;

/// <summary>
/// Daily sweep (02:00 UTC / 09:00 Hanoi) that cancels <see cref="OrderReturn"/>
/// rows whose buyer did not ship before <c>BuyerDecisionDueAt</c>. Raises
/// <see cref="OrderReturnExpiredEvent"/> so downstream handlers can notify the
/// buyer + seller. Escrow revocation is a V2 follow-up (plan M2).
/// </summary>
[DisallowConcurrentExecution]
internal sealed class OrderReturnDeadlineWatcherJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClock _clock;
    private readonly IOptionsMonitor<AppLoggingOptions> _loggingOptions;
    private readonly ILogger<OrderReturnDeadlineWatcherJob> _logger;

    public OrderReturnDeadlineWatcherJob(
        IServiceScopeFactory scopeFactory,
        IClock clock,
        IOptionsMonitor<AppLoggingOptions> loggingOptions,
        ILogger<OrderReturnDeadlineWatcherJob> logger)
    {
        _scopeFactory   = scopeFactory;
        _clock          = clock;
        _loggingOptions = loggingOptions;
        _logger         = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        using var scope = _scopeFactory.CreateScope();
        var dbContext  = scope.ServiceProvider.GetRequiredService<IDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var ct  = context.CancellationToken;
        var now = _clock.UtcNow;

        // Only Approved returns with an elapsed deadline and no ship yet.
        var eligible = await dbContext.Set<OrderReturn>()
            .Where(r => r.Status == OrderReturnStatus.Approved
                     && r.BuyerDecisionDueAt != null
                     && r.BuyerDecisionDueAt < now
                     && r.ShippedAt == null)
            .OrderBy(r => r.BuyerDecisionDueAt)
            .Take(200)
            .ToListAsync(ct);

        var expiredCount = 0;
        var failedCount  = 0;

        foreach (var orderReturn in eligible)
        {
            try
            {
                // Cancel via aggregate method (preserves audit fields).
                var cancelResult = orderReturn.Cancel(
                    reason: "Deadline expired: buyer did not ship",
                    nowUtc: now);

                if (cancelResult.IsFailure)
                {
                    failedCount++;
                    _logger.LogWarning(
                        "OrderReturnDeadlineWatcher: Cancel failed for OrderReturn {OrderReturnId}: {Error}",
                        orderReturn.Id.Value, cancelResult.Error.Message);
                    continue;
                }

                // Load the parent Order so we can raise the domain event from an
                // AggregateRoot (OrderReturn is a child entity).
                var order = await dbContext.Set<Order>()
                    .FirstOrDefaultAsync(o => o.Id == orderReturn.OrderId, ct);

                if (order is not null)
                {
                    order.RaiseOrderReturnExpired(orderReturn, now);
                    dbContext.Update(order);
                }

                dbContext.Update(orderReturn);
                await unitOfWork.SaveChangesAsync(ct);
                expiredCount++;

                // If this return had a deferred refund (from dispute resolution),
                // create an admin ticket so the stuck refund is surfaced for review.
                if (orderReturn.DeferredRefundIntent is not null
                    && orderReturn.DeferredRefundIntent != DeferredRefundIntent.None)
                {
                    try
                    {
                        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                        await sender.Send(new CreateAdminRefundRetryTicketCommand(
                            OrderId: orderReturn.OrderId.Value,
                            OrderReturnId: orderReturn.Id.Value,
                            Intent: orderReturn.DeferredRefundIntent.Id,
                            Amount: orderReturn.DeferredRefundAmount,
                            FailureReason: "Return expired: buyer did not ship within deadline. Deferred refund requires admin review."),
                            ct);

                        _logger.LogWarning(
                            "OrderReturnDeadlineWatcher: Deferred refund pending review — OrderReturn {OrderReturnId} expired with intent={Intent}, amount={Amount}",
                            orderReturn.Id.Value, orderReturn.DeferredRefundIntent.Id, orderReturn.DeferredRefundAmount);
                    }
                    catch (Exception ticketEx)
                    {
                        _logger.LogError(ticketEx,
                            "OrderReturnDeadlineWatcher: failed to create admin refund ticket for OrderReturn {OrderReturnId}",
                            orderReturn.Id.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                failedCount++;
                _logger.LogError(ex,
                    "OrderReturnDeadlineWatcher: error expiring OrderReturn {OrderReturnId}",
                    orderReturn.Id.Value);
            }
        }

        stopwatch.Stop();
        var logging = _loggingOptions.CurrentValue.Jobs;

        if (eligible.Count > 0 || failedCount > 0)
        {
            var level = failedCount > 0 || stopwatch.ElapsedMilliseconds >= logging.SlowJobThresholdMs
                ? LogLevel.Warning
                : LogLevel.Information;

            _logger.Log(
                level,
                "OrderReturnDeadlineWatcherJob completed in {DurationMs}ms. Eligible={Eligible}, Expired={Expired}, Failed={Failed}",
                stopwatch.ElapsedMilliseconds, eligible.Count, expiredCount, failedCount);
        }
        else if (logging.LogNoopRuns)
        {
            _logger.LogDebug(
                "OrderReturnDeadlineWatcherJob found no eligible returns in {DurationMs}ms.",
                stopwatch.ElapsedMilliseconds);
        }
    }
}
