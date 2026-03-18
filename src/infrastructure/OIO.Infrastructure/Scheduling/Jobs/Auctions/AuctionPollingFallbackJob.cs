using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.AuctionContext.Commands.ActivateAuction;
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
    private readonly IDbContext _dbContext;
    private readonly IMediator _mediator;
    private readonly IClock _clock;
    private readonly ILogger<AuctionPollingFallbackJob> _logger;

    public AuctionPollingFallbackJob(
        IDbContext dbContext,
        IMediator mediator,
        IClock clock,
        ILogger<AuctionPollingFallbackJob> logger)
    {
        _dbContext = dbContext;
        _mediator = mediator;
        _clock = clock;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var now = _clock.UtcNow;

        var overdueStarts = await _dbContext.Set<Auction>()
            .Where(a => a.Status == AuctionStatus.Pending && a.Info.StartTime <= now)
            .Select(a => a.Id)
            .ToListAsync(context.CancellationToken);

        foreach (var id in overdueStarts)
        {
            _logger.LogWarning("🛡️ Fallback: activating overdue auction {Id}.", id);
            await _mediator.Send(new ActivateAuctionCommand(id.Value), context.CancellationToken);
        }

        var overdueEnds = await _dbContext.Set<Auction>()
            .Where(a => a.Status == AuctionStatus.Active && a.Info.EndTime <= now)
            .Select(a => a.Id)
            .ToListAsync(context.CancellationToken);

        foreach (var id in overdueEnds)
        {
            _logger.LogWarning("🛡️ Fallback: ending overdue auction {Id}.", id);
            await _mediator.Send(new EndAuctionCommand(id.Value), context.CancellationToken);
        }

        if (overdueStarts.Count > 0 || overdueEnds.Count > 0)
        {
            _logger.LogWarning(
                "🛡️ Fallback processed: {Starts} starts, {Ends} ends.",
                overdueStarts.Count, overdueEnds.Count);
        }
    }
}