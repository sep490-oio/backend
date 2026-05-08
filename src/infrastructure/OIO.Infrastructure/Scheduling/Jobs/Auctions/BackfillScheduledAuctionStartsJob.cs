using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Scheduling;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Infrastructure.Persistence;

namespace OIO.Infrastructure.Scheduling.Jobs.Auctions;

/// <summary>
/// One-off backfill: on app startup, scan all Scheduled auctions whose StartTime is still in the future
/// and (re)schedule their start with IAuctionScheduler. Idempotent — safe to run repeatedly.
/// Replaces the legacy Publish step that previously scheduled auctions explicitly.
/// </summary>
public sealed class BackfillScheduledAuctionStartsJob : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackfillScheduledAuctionStartsJob> _logger;

    public BackfillScheduledAuctionStartsJob(
        IServiceScopeFactory scopeFactory,
        ILogger<BackfillScheduledAuctionStartsJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var scheduler = scope.ServiceProvider.GetRequiredService<IAuctionScheduler>();
            var clock = scope.ServiceProvider.GetRequiredService<IClock>();
            var nowUtc = clock.UtcNow;

            var auctions = await dbContext.Set<Auction>()
                .AsNoTracking()
                .Where(a => a.Status == AuctionStatus.Scheduled)
                .ToListAsync(cancellationToken);

            var pending = auctions
                .Where(a => a.Info != null && a.Info.StartTime > nowUtc)
                .ToList();

            _logger.LogInformation(
                "BackfillScheduledAuctionStartsJob: rescheduling {Count} scheduled auctions.",
                pending.Count);

            foreach (var auction in pending)
            {
                try
                {
                    await scheduler.ScheduleStartAsync(
                        auction.Id.Value,
                        auction.Info!.StartTime,
                        cancellationToken);

                    // Also (re)schedule the qualification close check if window hasn't ended yet.
                    if (auction.Info.Qualification is not null &&
                        !auction.Info.Qualification.HasClosed(nowUtc))
                    {
                        await scheduler.ScheduleQualificationCloseAsync(
                            auction.Id.Value,
                            auction.Info.Qualification.EndTime,
                            cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "BackfillScheduledAuctionStartsJob: failed to schedule start for auction {AuctionId}.",
                        auction.Id.Value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BackfillScheduledAuctionStartsJob: backfill failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
