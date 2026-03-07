using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Items;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;

namespace OIO.Infrastructure.BackgroundJobs;

public sealed class AuctionLifecycleJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuctionLifecycleJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);
    private readonly IClock _clock;

    public AuctionLifecycleJob(
        IServiceScopeFactory scopeFactory,
        IClock clock,
        ILogger<AuctionLifecycleJob> logger)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Auction lifecycle job started. Interval: {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAuctionsAsync(stoppingToken);
                await ProcessExpiredAuctionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in auction lifecycle job.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    /// <summary>
    /// Start auctions whose start_time has been reached.
    /// </summary>
    private async Task ProcessPendingAuctionsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var pendingAuctions = await dbContext.Set<Auction>()
            .Where(a => a.Status == AuctionStatus.Pending && a.Duration.StartTime <= _clock.UtcNow)
            .ToListAsync(ct);

        if (pendingAuctions.Count == 0) return;

        _logger.LogInformation(
            "Starting {Count} pending auctions.", pendingAuctions.Count);

        foreach (var auction in pendingAuctions)
        {
            try
            {
                auction.Start(_clock.UtcNow);
                _logger.LogInformation(
                    "Auction {AuctionId} started.", auction.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to start auction {AuctionId}.", auction.Id);
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
    }

    /// <summary>
    /// End auctions whose end_time has been reached.
    /// Determines winners and transitions item status.
    /// </summary>
    private async Task ProcessExpiredAuctionsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var expiredAuctions = await dbContext.Set<Auction>()
            .Include(a => a.Bids)
            .Include(a => a.AutoBids)
            .Where(a => a.Status == AuctionStatus.Active)
            .Where(a => a.Duration.EndTime <= _clock.UtcNow)
            .ToListAsync(ct);

        if (expiredAuctions.Count == 0) return;

        _logger.LogInformation(
            "Ending {Count} expired auctions.", expiredAuctions.Count);

        foreach (var auction in expiredAuctions)
        {
            try
            {
                auction.End(_clock.UtcNow);

                // Update item status based on auction outcome
                var item = await dbContext.GetByIdAsync<Item, ItemId>(
                    auction.ItemId,
                    cancellationToken: ct);
                if (item is not null)
                {
                    if (auction.CurrentWinnerId.HasValue && auction.IsReserveMet)
                    {
                        // Has winner and reserve met → item stays InAuction
                        // until payment confirmed (then → Sold)
                        _logger.LogInformation(
                            "Auction {AuctionId} ended with winner {WinnerId}. Price: {Price}",
                            auction.Id, auction.CurrentWinnerId, auction.CurrentPrice);
                    }
                    else
                    {
                        // No winner or reserve not met → return item to Active
                        item.ReturnToActive(_clock.UtcNow);

                        // Mark auction as failed
                        auction.MarkAsFailed(_clock.UtcNow);

                        _logger.LogInformation(
                            "Auction {AuctionId} ended with no valid winner. Item returned to active.",
                            auction.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to end auction {AuctionId}.", auction.Id);
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}