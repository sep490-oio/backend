using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Scheduling;
using OIO.Application.Context.AuctionContext.Commands.ActivateAuction;
using OIO.Application.Context.AuctionContext.Commands.EndAuction;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
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
                    // Cancel directly via domain method — CancelAuctionCommand requires
                    // ICurrentUser (seller ownership check) which is unavailable in background jobs.
                    await SystemCancelAuctionAsync(
                        dbContext, scope, id.Value,
                        $"Auto-cancelled: activation failed — {result.Error.Message}",
                        now, context.CancellationToken);
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

        // ── Phase 3 (safety net for CloseQualificationJob): auto-cancel when deposit phase ended with < 2 deposits ──
        // CloseQualificationJob fires precisely at Qualification.EndTime.
        // This scan catches any auctions that the per-auction timer missed.
        var noDepositCandidates = await dbContext.Set<Auction>()
            .Include(a => a.Deposits)
            .Include(a => a.Participants)
            .Include(a => a.BuyNowReservations)
            .Include(a => a.Item)
            .Where(a => a.Status == AuctionStatus.Scheduled
                        && a.Info != null && a.Info.Qualification!.EndTime <= now)
            .ToListAsync(context.CancellationToken);

        var cancelledNoDeposit = 0;
        foreach (var auction in noDepositCandidates)
        {
            // Match CloseQualificationJob logic: need >= 2 bid-eligible participants.
            if (auction.HasBidEligibleParticipants(now))
                continue;

            // Defer cancel if a buy-now reservation is still pending —
            // check IsPendingPayment (not IsActive) to cover the race window
            // between reservation expiry and ExpireBuyNowReservationsJob processing.
            var pendingBuyNow = auction.BuyNowReservations
                .FirstOrDefault(r => r.IsPendingPayment);
            if (pendingBuyNow is not null)
            {
                _logger.LogInformation(
                    "🛡️ Fallback: deferring cancel for auction {Id} — pending buy-now reservation {ReservationId}.",
                    auction.Id, pendingBuyNow.Id.Value);
                continue;
            }

            var heldCount = auction.Deposits.Count(d => d.IsHeld);

            _logger.LogWarning(
                "🛡️ Fallback: auto-cancelling auction {Id} — deposit phase ended with {Count} held deposit(s) (< 2 required).",
                auction.Id, heldCount);

            try
            {
                await SystemCancelAuctionAsync(
                    dbContext, scope, auction.Id.Value,
                    CloseQualificationJob.AutoCancelReason,
                    now, context.CancellationToken);
                cancelledNoDeposit++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "🛡️ Fallback: auto-cancel (insufficient deposits) failed for auction {Id}.", auction.Id);
            }
        }

        if (overdueStarts.Count > 0 || overdueEnds.Count > 0 || cancelledNoDeposit > 0)
        {
            _logger.LogWarning(
                "🛡️ Fallback processed: {Starts} starts, {Ends} ends, {NoDeposit} no-deposit cancellations.",
                overdueStarts.Count, overdueEnds.Count, cancelledNoDeposit);
        }
    }

    /// <summary>
    /// System-level auction cancellation for background jobs.
    /// Bypasses ICurrentUser ownership check since this runs without HTTP context.
    /// Mirrors CancelAuctionCommandHandler logic but without seller auth.
    /// </summary>
    private async Task SystemCancelAuctionAsync(
        IDbContext dbContext,
        IServiceScope scope,
        Guid auctionId,
        string reason,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var auction = await dbContext.GetByIdAsync<Auction, AuctionId>(
            AuctionId.From(auctionId),
            query => query.Include(a => a.Item),
            cancellationToken);

        if (auction is null)
        {
            _logger.LogWarning("🛡️ Fallback: auction {Id} not found for auto-cancel.", auctionId);
            return;
        }

        var cancelResult = auction.CancelAuction(reason, nowUtc);
        if (cancelResult.IsFailure)
        {
            _logger.LogWarning("🛡️ Fallback: auction {Id} cancel domain error: {Error}.",
                auctionId, cancelResult.Error.Message);
            return;
        }

        // Return item to active state
        var item = await dbContext.GetByIdAsync<Item, ItemId>(auction.ItemId, cancellationToken: cancellationToken);
        if (item is not null)
        {
            item.ReturnToActive(nowUtc);
        }

        // Cancel scheduled jobs
        var scheduler = scope.ServiceProvider.GetRequiredService<IAuctionScheduler>();
        await scheduler.CancelAsync(auctionId, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("🛡️ Fallback: auction {Id} auto-cancelled successfully. Reason: {Reason}",
            auctionId, reason);
    }
}
