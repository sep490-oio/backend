using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
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
/// Safety-net companion to <c>AuctionCompletionOnOrderCompletedHandler</c>. Each per-auction
/// iteration opens its own transaction, acquires the per-auction advisory lock, re-queries
/// dispute state under the lock, then calls <c>Auction.MarkCompleted</c>. The advisory lock
/// serialises against the event handler so only one side commits (plan change #1).
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
        var autoCompleteDays = App.Constraint.Auction.AutoCompleteDaysAfterDelivery;
        var cutoff = now.AddDays(-autoCompleteDays);

        // Find sold auctions — tracked=false here, we re-load under the per-auction lock below.
        var soldAuctions = await dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(a => a.Item)
            .Where(a => a.Status == AuctionStatus.Sold)
            .ToListAsync(context.CancellationToken);

        var completedCount = 0;
        var skippedOpenDisputeCount = 0;
        var skippedAlreadyAdvancedCount = 0;
        var skippedMarkCompletedFailureCount = 0;

        foreach (var candidate in soldAuctions)
        {
            // Bug #6 fix: previously used s.ClientOrderCode.Contains(auction.Id) which
            // (a) was unsargable (full table scan via ILIKE '%guid%'), (b) NEVER matched
            // GHN shipments because their ClientOrderCode is "OUT-{newGuid}" with no auction id,
            // and (c) could false-positive on substring collisions. Replaced with a proper
            // join via Order.AuctionId — both Order.AuctionId and OutboundShipment.OrderId are
            // indexed FKs.
            var deliveredShipment = await dbContext.Set<OutboundShipment>()
                .AsNoTracking()
                .Where(s => s.Status == OutboundShipmentStatus.Delivered &&
                            s.DeliveredAt.HasValue &&
                            s.DeliveredAt.Value <= cutoff &&
                            dbContext.Set<Order>().Any(o => o.Id == s.OrderId && o.AuctionId == candidate.Id))
                .FirstOrDefaultAsync(context.CancellationToken);

            if (deliveredShipment is null)
                continue;

            // Per-auction transaction scopes both the advisory lock and the fresh dispute re-query.
            // Using-disposal rolls back automatically if anything throws before CommitAsync.
            await using var transaction = await unitOfWork.BeginTransactionAsync(context.CancellationToken);

            try
            {
                // Step 1: advisory lock BEFORE re-reading (plan B2 change #1).
                await auctionLockRepo.AcquireAuctionLockAsync(candidate.Id, context.CancellationToken);

                // Step 2: re-load the auction tracked. Status may have flipped to Completed since the
                // upstream scan if the event handler committed in the interim; if so, skip.
                var auction = await dbContext.Set<Auction>()
                    .Include(a => a.Item)
                    .FirstOrDefaultAsync(a => a.Id == candidate.Id, context.CancellationToken);

                if (auction is null || auction.Status != AuctionStatus.Sold)
                {
                    skippedAlreadyAdvancedCount++;
                    await transaction.RollbackAsync(context.CancellationToken);
                    continue;
                }

                // Step 3: re-query dispute state inside this transaction (plan B2 change #2 —
                // replaces any stale check; covers the "dispute opened between scan and process" race).
                // Shared helper keeps the predicate in sync with AuctionCompletionOnOrderCompletedHandler.
                var hasOpenDispute = await DisputeStateQueries.HasOpenDisputeForAuctionAsync(
                    dbContext, auction.Id, context.CancellationToken);

                if (hasOpenDispute)
                {
                    skippedOpenDisputeCount++;
                    // Structured log doubles as the metric until an IMetrics abstraction lands.
                    _logger.LogInformation(
                        "metric=auction_autocomplete_skipped_due_to_open_dispute_total AuctionId={AuctionId}: open dispute, skipping.",
                        auction.Id.Value);
                    await transaction.RollbackAsync(context.CancellationToken);
                    continue;
                }

                // Mark item as sold, auction stays in sold state.
                // MarkSold is idempotent at the call site (we check Status != Sold above).
                // If MarkSold fails (e.g. item is in non-Sold non-InAuction state), log and skip.
                var markResult = auction.Item.MarkSold(now);
                if (markResult.IsFailure)
                {
                    _logger.LogWarning(
                        "AuctionAutoComplete: failed to MarkSold item {ItemId} for auction {AuctionId} (status={Status}): {Error}",
                        auction.Item.Id.Value, auction.Id.Value, auction.Item.Status.Id, markResult.Error.Message);
                    await transaction.RollbackAsync(context.CancellationToken);
                    continue;
                }

                // Step 4: advance Sold -> Completed (plan B2 change #3). Safe to pass hasOpenDispute:false
                // — we just read it under the advisory lock and the domain guard re-validates anyway.
                var completeResult = auction.MarkCompleted(
                    now,
                    deliveredShipment.DeliveredAt!.Value,
                    hasOpenDispute: false);

                if (completeResult.IsFailure)
                {
                    skippedMarkCompletedFailureCount++;
                    _logger.LogWarning(
                        "AuctionAutoComplete: MarkCompleted failed for AuctionId={AuctionId} (status={Status}): {ErrorCode} {ErrorMessage}. Continuing.",
                        auction.Id.Value, auction.Status.Id, completeResult.Error.Code, completeResult.Error.Message);
                    await transaction.RollbackAsync(context.CancellationToken);
                    continue;
                }

                await unitOfWork.SaveChangesAsync(context.CancellationToken);
                await transaction.CommitAsync(context.CancellationToken);

                completedCount++;
                _logger.LogInformation(
                    "metric=auction_completions_total source=job AuctionId={AuctionId} DeliveredAt={DeliveredAt}: " +
                    "auction transitioned Sold -> Completed.",
                    auction.Id.Value, deliveredShipment.DeliveredAt.Value);
            }
            catch (OperationCanceledException)
            {
                // Cooperative cancellation — let the job runtime handle the rollback.
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "AuctionAutoComplete: unexpected failure while processing AuctionId={AuctionId}. Rolling back.",
                    candidate.Id.Value);

                try { await transaction.RollbackAsync(context.CancellationToken); }
                catch { /* swallow — we are already in an error path */ }
            }
        }

        stopwatch.Stop();
        var logging = _loggingOptions.CurrentValue.Jobs;

        if (completedCount > 0 || skippedOpenDisputeCount > 0 || skippedMarkCompletedFailureCount > 0)
        {
            _logger.LogInformation(
                "AuctionAutoCompleteJob completed in {DurationMs}ms. SoldCandidates={SoldAuctionCount}, Completed={CompletedCount}, SkippedOpenDispute={SkippedOpenDisputeCount}, SkippedAlreadyAdvanced={SkippedAlreadyAdvancedCount}, SkippedMarkCompletedFailure={SkippedMarkCompletedFailureCount}, Cutoff={Cutoff}",
                stopwatch.ElapsedMilliseconds,
                soldAuctions.Count,
                completedCount,
                skippedOpenDisputeCount,
                skippedAlreadyAdvancedCount,
                skippedMarkCompletedFailureCount,
                cutoff);
        }
        else if (stopwatch.ElapsedMilliseconds >= logging.SlowJobThresholdMs)
        {
            _logger.LogWarning(
                "AuctionAutoCompleteJob completed with no changes in {DurationMs}ms. Cutoff={Cutoff}",
                stopwatch.ElapsedMilliseconds,
                cutoff);
        }
        else if (logging.LogNoopRuns)
        {
            _logger.LogDebug(
                "AuctionAutoCompleteJob found no auctions to auto-complete in {DurationMs}ms. Cutoff={Cutoff}",
                stopwatch.ElapsedMilliseconds,
                cutoff);
        }
    }
}
