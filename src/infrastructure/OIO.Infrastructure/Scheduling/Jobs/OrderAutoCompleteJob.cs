using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.OrderContext.Services;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using Quartz;

namespace OIO.Infrastructure.Scheduling.Jobs;

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
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var escrowSettlementService = scope.ServiceProvider.GetRequiredService<EscrowSettlementService>();

        var ct = context.CancellationToken;
        var now = _clock.UtcNow;
        var thresholdDate = now.AddDays(-3);

        // Find all orders that are Delivered and have been past the 3-day window
        // Load IDs only for memory efficiency (T007)
        var eligibleOrderIds = await dbContext.Set<Order>()
            .Where(o => o.Status == OrderStatus.Delivered && o.DeliveredAt <= thresholdDate)
            .Select(o => o.Id)
            .ToListAsync(ct);

        var completedCount = 0;
        var failedCount = 0;

        foreach (var orderId in eligibleOrderIds)
        {
            try
            {
                // Load full entity inside the loop (memory efficient for large batches)
                var order = await dbContext.Set<Order>()
                    .FirstOrDefaultAsync(o => o.Id == orderId, ct);

                if (order is null)
                {
                    _logger.LogWarning("Order {OrderId} not found, skipping.", orderId);
                    continue;
                }

                // EscrowSettlementService.ReleaseToSellerAsync handles both order.Complete()
                // and escrow release in a single operation — no need to call Complete() separately.
                var releaseResult = await escrowSettlementService.ReleaseToSellerAsync(
                    order, "Auto-completed: decision window expired", null, ct);

                if (releaseResult.IsFailure)
                {
                    failedCount++;
                    _logger.LogWarning(
                        "Failed to release escrow for Order {OrderId}: {Error}",
                        orderId, releaseResult.Error);
                    continue;
                }

                await unitOfWork.SaveChangesAsync(ct);
                completedCount++;
            }
            catch (Exception ex)
            {
                failedCount++;
                _logger.LogError(ex, "Error auto-completing Order {OrderId}", orderId);
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
