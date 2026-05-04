using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using Quartz;

namespace OIO.Infrastructure.Scheduling.Jobs;

/// <summary>
/// Reconciliation safety-net for the Sold -> Completed transition.
///
/// Eligibility (changed from previous OutboundShipment-only filter):
///   - Auction.Status == Sold
///   - Linked Order.Status == Completed (escrow already released by ConfirmAsync)
///   - No open disputes
///
/// The Order.Status == Completed gate ensures this job NEVER bypasses the order
/// or escrow flow — the canonical post-delivery path is now
/// <c>OrderAutoCompleteJob</c> -> <c>IOrderReceiptService.ConfirmAsync</c> ->
/// <c>OrderCompletedEvent</c> -> <c>AuctionCompletionOnOrderCompletedHandler</c>.
/// This job only catches auctions that were left at Sold because the event
/// handler crashed/missed; it never independently completes a Delivered order.
/// </summary>
[DisallowConcurrentExecution]
public sealed class AuctionAutoCompleteJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClock _clock;
    private readonly IOptionsMonitor<AppLoggingOptions> _loggingOptions;
    private readonly ILogger<AuctionAutoCompleteJob> _logger;

    public AuctionAutoCompleteJob(
        IServiceScopeFactory scopeFactory,
        IClock clock,
        IOptionsMonitor<AppLoggingOptions> loggingOptions,
        ILogger<AuctionAutoCompleteJob> logger)
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
        var auctionLockRepo = scope.ServiceProvider.GetRequiredService<IAuctionLockRepository>();

        var now = _clock.UtcNow;

        // Reconciliation eligibility: auction stuck at Sold WHILE its linked
        // Order is already Completed. We do NOT fall back to OutboundShipment
        // delivery-window heuristics — the OrderAutoCompleteJob is now the
        // canonical Delivered -> Completed mover, and this job only patches up
        // the Sold -> Completed step if the event handler missed it.
        var soldAuctions = await (
            from a in dbContext.Set<Auction>().AsNoTracking().Include(a => a.Item)
            join o in dbContext.Set<Order>().AsNoTracking() on a.Id equals o.AuctionId
            where a.Status == AuctionStatus.Sold
                  && o.Status == OrderStatus.Completed
            select new { Auction = a, OrderCompletedAt = o.CompletedAt })
            .ToListAsync(context.CancellationToken);

        var completedCount = 0;
        var skippedOpenDisputeCount = 0;
        var skippedAlreadyAdvancedCount = 0;
        var skippedMarkCompletedFailureCount = 0;

        foreach (var candidate in soldAuctions)
        {
            try
            {
                // Per-auction work runs through unitOfWork.ExecuteInTransactionAsync so the
                // Npgsql retrying execution strategy can replay it as a unit. The strategy
                // refuses raw user-initiated transactions otherwise.
                await unitOfWork.ExecuteInTransactionAsync(async ct =>
                {
                    // Step 1: advisory lock BEFORE re-reading (plan B2 change #1).
                    await auctionLockRepo.AcquireAuctionLockAsync(candidate.Auction.Id, ct);

                    // Step 2: re-load the auction tracked. Status may have flipped to Completed since the
                    // upstream scan if the event handler committed in the interim; if so, skip.
                    var auction = await dbContext.Set<Auction>()
                        .Include(a => a.Item)
                        .FirstOrDefaultAsync(a => a.Id == candidate.Auction.Id, ct);

                    if (auction is null || auction.Status != AuctionStatus.Sold)
                    {
                        skippedAlreadyAdvancedCount++;
                        return;
                    }

                    // Step 3: re-query dispute state inside this transaction (plan B2 change #2 —
                    // replaces any stale check; covers the "dispute opened between scan and process" race).
                    // Shared helper keeps the predicate in sync with AuctionCompletionOnOrderCompletedHandler.
                    var hasOpenDispute = await DisputeStateQueries.HasOpenDisputeForAuctionAsync(
                        dbContext, auction.Id, ct);

                    if (hasOpenDispute)
                    {
                        skippedOpenDisputeCount++;
                        // Structured log doubles as the metric until an IMetrics abstraction lands.
                        _logger.LogInformation(
                            "metric=auction_autocomplete_skipped_due_to_open_dispute_total AuctionId={AuctionId}: open dispute, skipping.",
                            auction.Id.Value);
                        return;
                    }

                    // Mark item as sold before completing the auction. MarkSold is idempotent
                    // when the AuctionSoldEventHandler already advanced the item to Sold.
                    // If MarkSold fails (e.g. item is in non-Sold non-InAuction state), log and skip.
                    var markResult = auction.Item.MarkSold(now);
                    if (markResult.IsFailure)
                    {
                        _logger.LogWarning(
                            "AuctionAutoComplete: failed to MarkSold item {ItemId} for auction {AuctionId} (status={Status}): {Error}",
                            auction.Item.Id.Value, auction.Id.Value, auction.Item.Status.Id, markResult.Error.Message);
                        return;
                    }

                    // Step 4: advance Sold -> Completed (plan B2 change #3). The completion timestamp is
                    // sourced from the linked Order.CompletedAt — that's the authoritative moment escrow
                    // released, not a fresh `now` from the job tick.
                    var completeAt = candidate.OrderCompletedAt ?? now;
                    var completeResult = auction.MarkCompleted(
                        now,
                        completeAt,
                        hasOpenDispute: false);

                    if (completeResult.IsFailure)
                    {
                        skippedMarkCompletedFailureCount++;
                        _logger.LogWarning(
                            "AuctionAutoComplete: MarkCompleted failed for AuctionId={AuctionId} (status={Status}): {ErrorCode} {ErrorMessage}. Continuing.",
                            auction.Id.Value, auction.Status.Id, completeResult.Error.Code, completeResult.Error.Message);
                        return;
                    }

                    await unitOfWork.SaveChangesAsync(ct);

                    completedCount++;
                    _logger.LogInformation(
                        "metric=auction_completions_total source=reconciliation_job AuctionId={AuctionId} OrderCompletedAt={OrderCompletedAt}: " +
                        "auction reconciled Sold -> Completed (event handler missed this transition).",
                        auction.Id.Value, completeAt);
                }, context.CancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Cooperative cancellation — let the job runtime handle teardown.
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "AuctionAutoComplete: unexpected failure while processing AuctionId={AuctionId}. Rolling back.",
                    candidate.Auction.Id.Value);
            }
            finally
            {
                // Drop any in-memory mutations that didn't make it to SaveChangesAsync so
                // they don't leak into the next iteration's commit.
                dbContext.DetachAll();
            }
        }

        stopwatch.Stop();
        var logging = _loggingOptions.CurrentValue.Jobs;

        if (completedCount > 0 || skippedOpenDisputeCount > 0 || skippedMarkCompletedFailureCount > 0)
        {
            _logger.LogInformation(
                "AuctionAutoCompleteJob (reconciliation) completed in {DurationMs}ms. SoldCandidates={SoldAuctionCount}, Completed={CompletedCount}, SkippedOpenDispute={SkippedOpenDisputeCount}, SkippedAlreadyAdvanced={SkippedAlreadyAdvancedCount}, SkippedMarkCompletedFailure={SkippedMarkCompletedFailureCount}",
                stopwatch.ElapsedMilliseconds,
                soldAuctions.Count,
                completedCount,
                skippedOpenDisputeCount,
                skippedAlreadyAdvancedCount,
                skippedMarkCompletedFailureCount);
        }
        else if (stopwatch.ElapsedMilliseconds >= logging.SlowJobThresholdMs)
        {
            _logger.LogWarning(
                "AuctionAutoCompleteJob (reconciliation) completed with no changes in {DurationMs}ms.",
                stopwatch.ElapsedMilliseconds);
        }
        else if (logging.LogNoopRuns)
        {
            _logger.LogDebug(
                "AuctionAutoCompleteJob (reconciliation) found no Sold auctions paired with a Completed order in {DurationMs}ms.",
                stopwatch.ElapsedMilliseconds);
        }
    }
}
