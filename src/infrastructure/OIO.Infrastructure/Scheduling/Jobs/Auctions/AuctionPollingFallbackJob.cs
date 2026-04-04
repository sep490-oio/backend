using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.AuctionContext.Commands.ActivateAuction;
using OIO.Application.Context.AuctionContext.Commands.CancelAuction;
using OIO.Application.Context.AuctionContext.Commands.EndAuction;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using Quartz;

namespace OIO.Infrastructure.Scheduling.Jobs.Auctions;

/// <summary>
/// Safety net polling job. Runs every 60s (relaxed, not 10s).
/// Catches any auctions that per-auction timers missed
/// (timer failure, race condition, etc.)
///
/// In normal operation, this job finds NOTHING to do.
/// </summary>
[DisallowConcurrentExecution]
public sealed class AuctionPollingFallbackJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClock _clock;
    private readonly ILogger<AuctionPollingFallbackJob> _logger;

    public AuctionPollingFallbackJob(
        IServiceScopeFactory scopeFactory,
        IClock clock,
        ILogger<AuctionPollingFallbackJob> logger)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var now = _clock.UtcNow;

        var overdueStarts = await dbContext.Set<Auction>()
            .Where(a => a.Status == AuctionStatus.Scheduled && a.Info != null && a.Info.StartTime <= now)
            .Select(a => a.Id)
            .ToListAsync(context.CancellationToken);

        foreach (var id in overdueStarts)
        {
            _logger.LogWarning("🛡️ Fallback: activating overdue auction {Id}.", id);
            var result = await mediator.Send(new ActivateAuctionCommand(id.Value), context.CancellationToken);
            if (result.IsFailure)
            {
                _logger.LogError(
                    "🛡️ Fallback: activation FAILED for auction {Id}: {Error}. Auto-cancelling.",
                    id, result.Error.Message);
                try
                {
                    await mediator.Send(
                        new CancelAuctionCommand(id.Value, $"Auto-cancelled: activation failed — {result.Error.Message}"),
                        context.CancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "🛡️ Fallback: auto-cancel also failed for auction {Id}.", id);
                }
            }
        }

        var overdueEnds = await dbContext.Set<Auction>()
            .Where(a => a.Status == AuctionStatus.Active && a.Info != null && a.Info.EndTime <= now)
            .Select(a => a.Id)
            .ToListAsync(context.CancellationToken);

        foreach (var id in overdueEnds)
        {
            _logger.LogWarning("🛡️ Fallback: ending overdue auction {Id}.", id);
            var result = await mediator.Send(new EndAuctionCommand(id.Value), context.CancellationToken);
            if (result.IsFailure)
            {
                _logger.LogError(
                    "🛡️ Fallback: ending FAILED for auction {Id}: {Error}.",
                    id, result.Error.Message);
            }
        }

        if (overdueStarts.Count > 0 || overdueEnds.Count > 0)
        {
            _logger.LogWarning(
                "🛡️ Fallback processed: {Starts} starts, {Ends} ends.",
                overdueStarts.Count, overdueEnds.Count);
        }
    }
}
