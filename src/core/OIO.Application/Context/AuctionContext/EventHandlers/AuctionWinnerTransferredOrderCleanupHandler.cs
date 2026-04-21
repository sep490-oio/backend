using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

/// <summary>
/// Bug #9 fix: when an auction's win is transferred to the runner-up, the previous
/// winner's PendingPayment order is cancelled before the new order is created.
/// Without this, two Order rows would coexist for the same auction (the defaulted
/// winner's stale order + the runner-up's new order), with no automated cleanup.
/// </summary>
internal sealed class AuctionWinnerTransferredOrderCleanupHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<AuctionWinnerTransferredOrderCleanupHandler> logger)
    : INotificationHandler<AuctionWinnerTransferredEvent>
{
    public async Task Handle(AuctionWinnerTransferredEvent notification, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(notification.AuctionId, out var auctionGuid) ||
            !Guid.TryParse(notification.PreviousWinnerId, out var previousWinnerGuid))
        {
            logger.LogWarning(
                "AuctionWinnerTransferred: skipping cleanup — invalid GUIDs in event. AuctionId={AuctionId}, PreviousWinnerId={PreviousWinnerId}",
                notification.AuctionId, notification.PreviousWinnerId);
            return;
        }

        var auctionId = AuctionId.From(auctionGuid);
        var previousWinnerId = UserId.From(previousWinnerGuid);

        // Cancel the previous winner's PendingPayment order(s) for this auction.
        var staleOrders = await dbContext.Set<Order>()
            .Where(o =>
                o.AuctionId == auctionId &&
                o.BuyerId == previousWinnerId &&
                o.Status == OrderStatus.PendingPayment)
            .ToListAsync(cancellationToken);

        if (staleOrders.Count == 0)
        {
            logger.LogInformation(
                "AuctionWinnerTransferred: no stale orders to cancel for AuctionId={AuctionId}, PreviousWinnerId={PreviousWinnerId}",
                auctionGuid, previousWinnerGuid);
            return;
        }

        var nowUtc = clock.UtcNow;
        var cancelledCount = 0;
        foreach (var order in staleOrders)
        {
            var result = order.Cancel(
                "Auction win transferred to runner-up after this winner defaulted on payment.",
                nowUtc);
            if (result.IsFailure)
            {
                logger.LogWarning(
                    "AuctionWinnerTransferred: failed to cancel stale order {OrderId}: {Error}",
                    order.Id.Value, result.Error.Message);
                continue;
            }
            cancelledCount++;
        }

        if (cancelledCount > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "AuctionWinnerTransferred: cancelled {Count} stale order(s) for AuctionId={AuctionId}, PreviousWinnerId={PreviousWinnerId}",
                cancelledCount, auctionGuid, previousWinnerGuid);
        }
    }
}
