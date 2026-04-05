using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<OrderAutoCompleteJob> _logger;

    public OrderAutoCompleteJob(
        IServiceScopeFactory scopeFactory,
        IClock clock,
        ILogger<OrderAutoCompleteJob> logger)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var escrowSettlementService = scope.ServiceProvider.GetRequiredService<EscrowSettlementService>();

        _logger.LogInformation("Starting OrderAutoCompleteJob...");

        var ct = context.CancellationToken;
        var now = _clock.UtcNow;
        var autoCompletedThreshold = now.AddDays(-7);

        // Find all Delivered orders whose 7-day auto-complete window has expired
        var eligibleOrderIds = await dbContext.Set<Order>()
            .Where(o => o.Status == OrderStatus.Delivered
                        && o.DeliveredAt.HasValue
                        && o.DeliveredAt.Value <= autoCompletedThreshold)
            .Select(o => o.Id)
            .ToListAsync(ct);

        _logger.LogInformation("Found {Count} orders eligible for auto-completion.", eligibleOrderIds.Count);

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
                    _logger.LogWarning(
                        "Failed to release escrow for Order {OrderId}: {Error}",
                        orderId, releaseResult.Error);
                    continue;
                }

                await unitOfWork.SaveChangesAsync(ct);

                _logger.LogInformation("Auto-completed Order {OrderId}", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-completing Order {OrderId}", orderId);
            }
        }

        _logger.LogInformation("OrderAutoCompleteJob finished.");
    }
}
