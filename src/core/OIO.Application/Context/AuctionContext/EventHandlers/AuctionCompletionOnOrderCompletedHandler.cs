using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Aggregates.Orders.Events;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

/// <summary>
/// Reacts to <see cref="OrderCompletedEvent"/> by advancing the linked auction from Sold to
/// Completed. Serves as the primary authority for the Sold -> Completed transition; the
/// <c>AuctionAutoCompleteJob</c> remains as an idempotent safety net.
///
/// Ordering inside the unit-of-work is load-bearing (plan B1):
///   1. Resolve <c>AuctionId</c> from the Order aggregate (OrderCompletedEvent does not carry it).
///   2. Open a transaction + acquire the per-auction advisory lock BEFORE reading the auction.
///   3. Re-read the auction under the lock, query dispute state in the same UoW.
///   4. Call <see cref="Auction.MarkCompleted"/>; domain guards re-validate all invariants.
///   5. Commit. The advisory lock releases automatically on commit/rollback.
///
/// Idempotency across event replays is provided by the outbox's
/// <c>IdempotentDomainEventHandler&lt;T&gt;</c> decorator, registered by
/// <c>OIO.Infrastructure.DependencyInjection.DecorateRegisteredNotificationHandlers</c>.
/// </summary>
internal sealed class AuctionCompletionOnOrderCompletedHandler
    : INotificationHandler<OrderCompletedEvent>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuctionLockRepository _auctionLockRepo;
    private readonly IClock _clock;
    private readonly ILogger<AuctionCompletionOnOrderCompletedHandler> _logger;

    public AuctionCompletionOnOrderCompletedHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IAuctionLockRepository auctionLockRepo,
        IClock clock,
        ILogger<AuctionCompletionOnOrderCompletedHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _auctionLockRepo = auctionLockRepo;
        _clock = clock;
        _logger = logger;
    }

    public async Task Handle(OrderCompletedEvent notification, CancellationToken cancellationToken)
    {
        var orderId = OrderId.From(Guid.Parse(notification.OrderId));

        // Step 1: Resolve AuctionId via the Order aggregate — OrderCompletedEvent does not carry it
        // directly (see plan Surprise #2).
        var order = await _dbContext.GetByIdAsync<Order, OrderId>(
            id: orderId,
            queryBuilder: query => query.AsNoTracking(),
            cancellationToken: cancellationToken);

        if (order is null)
        {
            _logger.LogWarning(
                "AuctionCompletionOnOrderCompleted: order {OrderId} not found; skipping.",
                notification.OrderId);
            return;
        }

        var auctionId = order.AuctionId;

        // Step 2: Open a transaction + acquire the advisory lock BEFORE the auction read.
        // pg_advisory_xact_lock requires an ambient transaction (AcquireAuctionLockAsync asserts this).
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        await _auctionLockRepo.AcquireAuctionLockAsync(auctionId, cancellationToken);

        // Step 3: Load the auction tracked — we mutate Status on success.
        var auction = await _dbContext.Set<Auction>()
            .Include(a => a.Item)
            .FirstOrDefaultAsync(a => a.Id == auctionId, cancellationToken);

        if (auction is null)
        {
            _logger.LogWarning(
                "AuctionCompletionOnOrderCompleted: auction {AuctionId} for order {OrderId} not found; skipping.",
                auctionId.Value, notification.OrderId);
            return;
        }

        // Step 4: Query dispute state inside the same UoW. Shared with AuctionAutoCompleteJob
        // via DisputeStateQueries so new dispute statuses stay in sync across both authorities.
        var hasOpenDispute = await DisputeStateQueries.HasOpenDisputeForAuctionAsync(
            _dbContext, auctionId, cancellationToken);

        // Step 5: Domain guards re-validate every invariant for defence in depth.
        var result = auction.MarkCompleted(_clock.UtcNow, notification.CompletedAt, hasOpenDispute);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Auction.MarkCompleted.DisputeOpen")
            {
                // Structured log doubles as the dispute-skip metric until an IMetrics abstraction lands.
                // Dashboard/alerts can aggregate on the event name. See B1 follow-up.
                _logger.LogInformation(
                    "metric=auction_completion_skipped_due_to_dispute_total source=event_handler AuctionId={AuctionId} OrderId={OrderId}: " +
                    "dispute open, leaving auction Sold so the auto-complete job can retry after resolution.",
                    auctionId.Value, orderId.Value);
                return;
            }

            // NoWinner / ItemNotSold / transition-invalid are genuine data anomalies — the job
            // will re-hit the same guard, so this is effectively silent data loss without an alert.
            // Emit at Error level with a structured event name so observability can alert on it.
            _logger.LogError(
                "metric=auction_completion_anomaly_total source=event_handler AuctionId={AuctionId} OrderId={OrderId} " +
                "status={Status} itemStatus={ItemStatus} code={ErrorCode}: {ErrorMessage}. " +
                "Returning without Completed to avoid retrying a domain-invariant violation; investigate promptly.",
                auctionId.Value, orderId.Value, auction.Status.Id, auction.Item.Status.Id,
                result.Error.Code, result.Error.Message);
            return;
        }

        // Step 6: Commit. Advisory lock releases here.
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        // Structured log doubles as the completions metric until an IMetrics abstraction lands.
        _logger.LogInformation(
            "metric=auction_completions_total source=event_handler AuctionId={AuctionId} OrderId={OrderId} DeliveredAt={DeliveredAt}: " +
            "auction transitioned Sold -> Completed.",
            auctionId.Value, orderId.Value, notification.CompletedAt);
    }
}
