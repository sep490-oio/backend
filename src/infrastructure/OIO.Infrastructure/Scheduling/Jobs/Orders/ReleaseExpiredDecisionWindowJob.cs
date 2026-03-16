using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Context.OrderContext.Services;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Infrastructure.Persistence;

namespace OIO.Infrastructure.Scheduling.Jobs.Orders;

public sealed class ReleaseExpiredDecisionWindowJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReleaseExpiredDecisionWindowJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(10);

    public ReleaseExpiredDecisionWindowJob(
        IServiceScopeFactory scopeFactory,
        ILogger<ReleaseExpiredDecisionWindowJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReleaseExpiredAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while releasing expired decision-window orders.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ReleaseExpiredAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var settlementService = scope.ServiceProvider.GetRequiredService<EscrowSettlementService>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var nowUtc = clock.UtcNow;
        var candidateOrders = await dbContext.Set<Order>()
            .Include(x => x.Return)
            .Include(x => x.Escrows)
            .Where(x =>
                x.Status == OrderStatus.Delivered &&
                x.DisputedAt == null &&
                x.DecisionWindowEndsAt != null &&
                x.DecisionWindowEndsAt <= nowUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var order in candidateOrders)
        {
            if (order.Return is not null &&
                order.Return.Status != OrderReturnStatus.Rejected &&
                order.Return.Status != OrderReturnStatus.Cancelled &&
                order.Return.Status != OrderReturnStatus.Resolved)
            {
                continue;
            }

            var result = await settlementService.ReleaseToSellerAsync(
                order,
                "Decision window expired without return or dispute",
                actorId: null,
                cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to release escrow for order {OrderId}: {Error}",
                    order.Id.Value,
                    result.Error.Message);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
