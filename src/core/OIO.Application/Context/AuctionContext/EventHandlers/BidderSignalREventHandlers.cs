using MediatR;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.UserContext.Hubs;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

/// <summary>
/// Pushes bid status changes and auction outcomes to individual bidders via UserHub.
/// This complements the existing AuctionHub (which broadcasts to auction room viewers).
/// </summary>
internal sealed class BidderSignalREventHandlers :
    INotificationHandler<OutbidEvent>,
    INotificationHandler<AuctionEndedEvent>
{
    private readonly IDbContext _dbContext;
    private readonly IUserNotificationService _userNotificationService;

    public BidderSignalREventHandlers(
        IDbContext dbContext,
        IUserNotificationService userNotificationService)
    {
        _dbContext = dbContext;
        _userNotificationService = userNotificationService;
    }

    /// <summary>
    /// When a bidder is outbid, push BidStatusChanged to them via UserHub.
    /// </summary>
    public async Task Handle(OutbidEvent notification, CancellationToken ct)
    {
        var outbidUserId = Guid.Parse(notification.OutbidBidderId);

        await _userNotificationService.NotifyBidStatusChangedAsync(
            outbidUserId,
            new BidStatusChangedNotification(
                AuctionId: Guid.Parse(notification.AuctionId),
                BidId: Guid.Empty, // bid ID not in event — FE will refetch
                NewStatus: "outbid",
                CurrentPrice: notification.NewHighestBid,
                Currency: "VND"));
    }

    /// <summary>
    /// When an auction ends, notify all bidders of the outcome (won/lost/failed).
    /// </summary>
    public async Task Handle(AuctionEndedEvent notification, CancellationToken ct)
    {
        var auctionId = AuctionId.From(Guid.Parse(notification.AuctionId));

        var auction = await _dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(a => a.Bids)
            .Include(a => a.Item)
            .FirstOrDefaultAsync(a => a.Id == auctionId, ct);

        if (auction is null) return;

        // Get unique bidder IDs
        var bidderIds = auction.Bids
            .Select(b => b.BidderId.Value)
            .Distinct()
            .ToList();

        var winnerId = notification.WinnerId != null ? Guid.Parse(notification.WinnerId) : (Guid?)null;
        var title = auction.Item.Title.Value;

        foreach (var bidderId in bidderIds)
        {
            var outcome = bidderId == winnerId ? "won" : "lost";

            await _userNotificationService.NotifyAuctionOutcomeAsync(
                bidderId,
                new AuctionOutcomeNotification(
                    AuctionId: auctionId.Value,
                    AuctionTitle: title,
                    Outcome: outcome,
                    FinalPrice: notification.FinalPrice,
                    Currency: auction.Pricing.Currency.Id));
        }
    }
}
