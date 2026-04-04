using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using Quartz;

namespace OIO.Infrastructure.Scheduling.Jobs;

/// <summary>
/// Auto-completes auctions where:
/// - Status is Sold
/// - The outbound shipment has been delivered
/// - N days have passed since delivery
/// - No open disputes exist
/// </summary>
[DisallowConcurrentExecution]
public sealed class AuctionAutoCompleteJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClock _clock;
    private readonly ILogger<AuctionAutoCompleteJob> _logger;

    public AuctionAutoCompleteJob(
        IServiceScopeFactory scopeFactory,
        IClock clock,
        ILogger<AuctionAutoCompleteJob> logger)
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

        var now = _clock.UtcNow;
        var autoCompleteDays = App.Constraint.Auction.AutoCompleteDaysAfterDelivery;
        var cutoff = now.AddDays(-autoCompleteDays);

        _logger.LogInformation("Running AuctionAutoCompleteJob. Cutoff: {Cutoff}", cutoff);

        // Find sold auctions
        var soldAuctions = await dbContext.Set<Auction>()
            .Include(a => a.Item)
            .Where(a => a.Status == AuctionStatus.Sold)
            .ToListAsync(context.CancellationToken);

        if (soldAuctions.Count == 0)
        {
            _logger.LogDebug("No sold auctions found for auto-completion.");
            return;
        }

        var completedCount = 0;

        foreach (var auction in soldAuctions)
        {
            // Check if there's a delivered outbound shipment past the cutoff
            var deliveredShipment = await dbContext.Set<OutboundShipment>()
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                        s.ClientOrderCode.Contains(auction.Id.Value.ToString()) &&
                        s.Status == OutboundShipmentStatus.Delivered &&
                        s.DeliveredAt.HasValue &&
                        s.DeliveredAt.Value <= cutoff,
                    context.CancellationToken);

            if (deliveredShipment is null)
                continue;

            // Check if there are any open disputes for this auction
            var hasOpenDispute = await dbContext.Set<Dispute>()
                .AsNoTracking()
                .AnyAsync(d =>
                        d.AuctionId == auction.Id &&
                        d.Status != DisputeStatus.Resolved &&
                        d.Status != DisputeStatus.Closed &&
                        d.Status != DisputeStatus.Cancelled,
                    context.CancellationToken);

            if (hasOpenDispute)
            {
                _logger.LogDebug("Auction {AuctionId} has open disputes, skipping auto-complete.", auction.Id);
                continue;
            }

            _logger.LogInformation(
                "Auto-completing auction {AuctionId}. Delivered at {DeliveredAt}, cutoff {Cutoff}.",
                auction.Id, deliveredShipment.DeliveredAt, cutoff);

            // Mark item as sold, auction stays in sold state
            // The payment to seller should be triggered here
            auction.Item.MarkSold(now);
            completedCount++;
        }

        if (completedCount > 0)
        {
            await unitOfWork.SaveChangesAsync(context.CancellationToken);
        }

        _logger.LogInformation("AuctionAutoCompleteJob finished. Auto-completed {Count} auctions.", completedCount);
    }
}
