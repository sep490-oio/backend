using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Descriptions;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class AuctionCancelledAutoBidFundsReleaseEventHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<AuctionCancelledAutoBidFundsReleaseEventHandler> logger)
    : INotificationHandler<AuctionCancelledEvent>
{
    public Task Handle(AuctionCancelledEvent notification, CancellationToken cancellationToken)
    {
        return AuctionAutoBidFundsReleaseDispatch.ReleaseAutoBidFundsAsync(
            dbContext,
            unitOfWork,
            clock,
            logger,
            Guid.Parse(notification.AuctionId),
            $"Auction cancelled: {notification.Reason}",
            cancellationToken);
    }
}

internal sealed class AuctionFailedAutoBidFundsReleaseEventHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<AuctionFailedAutoBidFundsReleaseEventHandler> logger)
    : INotificationHandler<AuctionFailedEvent>
{
    public Task Handle(AuctionFailedEvent notification, CancellationToken cancellationToken)
    {
        return AuctionAutoBidFundsReleaseDispatch.ReleaseAutoBidFundsAsync(
            dbContext,
            unitOfWork,
            clock,
            logger,
            Guid.Parse(notification.AuctionId),
            $"Auction failed: {notification.Reason}",
            cancellationToken);
    }
}

internal sealed class AuctionSoldAutoBidFundsReleaseEventHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<AuctionSoldAutoBidFundsReleaseEventHandler> logger)
    : INotificationHandler<AuctionSoldEvent>
{
    public Task Handle(AuctionSoldEvent notification, CancellationToken cancellationToken)
    {
        return AuctionAutoBidFundsReleaseDispatch.ReleaseAutoBidFundsAsync(
            dbContext,
            unitOfWork,
            clock,
            logger,
            Guid.Parse(notification.AuctionId),
            "Auction sold: auto-bid reservation released.",
            cancellationToken);
    }
}

internal sealed class AuctionTerminatedAutoBidFundsReleaseEventHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<AuctionTerminatedAutoBidFundsReleaseEventHandler> logger)
    : INotificationHandler<AuctionTerminatedEvent>
{
    public Task Handle(AuctionTerminatedEvent notification, CancellationToken cancellationToken)
    {
        return AuctionAutoBidFundsReleaseDispatch.ReleaseAutoBidFundsAsync(
            dbContext,
            unitOfWork,
            clock,
            logger,
            Guid.Parse(notification.AuctionId),
            $"Auction terminated: {notification.Reason}",
            cancellationToken);
    }
}

internal static class AuctionAutoBidFundsReleaseDispatch
{
    public static async Task ReleaseAutoBidFundsAsync(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger logger,
        Guid auctionId,
        string reason,
        CancellationToken cancellationToken)
    {
        var candidates = await dbContext.Set<AutoBid>()
            .AsNoTracking()
            .Where(x => x.AuctionId == AuctionId.From(auctionId))
            .Select(x => new AutoBidFundsReleaseCandidate(x.BidderId, x.HeldAmount))
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
            return;

        var now = clock.UtcNow;
        var hasChanges = false;

        foreach (var candidate in candidates)
        {
            var wallet = await dbContext.Set<Wallet>()
                .FirstOrDefaultAsync(x => x.UserId == candidate.BidderId, cancellationToken);

            if (wallet is null)
                continue;

            var unholdResult = wallet.Unhold(
                candidate.Amount,
                transactionId: null,
                description: LedgerDescriptions.AutoBidReservationReleased(auctionId, reason),
                nowUtc: now);

            if (unholdResult.IsSuccess)
            {
                hasChanges = true;
                continue;
            }

            logger.LogWarning(
                "Failed to release auto-bid reservation for auction {AuctionId}, bidder {BidderId}. Error={Error}",
                auctionId,
                candidate.BidderId.Value,
                unholdResult.Error.Message);
        }

        if (hasChanges)
            await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private sealed record AutoBidFundsReleaseCandidate(
        OIO.Domain.Context.UserContext.ValueObjects.Ids.UserId BidderId,
        decimal Amount);
}
