using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.NotificationContext.Enums;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class OutbidEventHandler
    : INotificationHandler<OutbidEvent>
{
    private readonly IDbContext _dbContext;
    private readonly ISender _sender;
    private readonly ILogger<OutbidEventHandler> _logger;

    public OutbidEventHandler(
        IDbContext dbContext,
        ISender sender,
        ILogger<OutbidEventHandler> logger)
    {
        _dbContext = dbContext;
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(OutbidEvent notification, CancellationToken ct)
    {
        var auctionId = AuctionId.From(Guid.Parse(notification.AuctionId));

        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            auctionId,
            queryBuilder: query => query
                .AsNoTracking()
                .Include(a => a.Bids),
            cancellationToken: ct);

        if (auction is null)
        {
            _logger.LogWarning(
                "Skipped outbid notification because auction {AuctionId} could not be loaded.",
                notification.AuctionId);
            return;
        }

        _logger.LogInformation(
            "Notifying outbid: Auction={AuctionId}, OutbidUser={OutbidUser}",
            notification.AuctionId, notification.OutbidBidderId);

        // Create persistent bell notification for outbid bidder
        var itemTitle = auction.Item?.Title?.Value ?? "Auction";
        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: Guid.Parse(notification.OutbidBidderId),
                NotificationType: "auction",
                EventType: "auction_outbid",
                Title: "Ban da bi vuot gia!",
                Message: $"Phien dau gia \"{itemTitle}\" da co gia moi: {auction.Pricing.CurrentAmount}. Dat gia cao hon de gianh lai.",
                Priority: NotificationPriority.High,
                EntityType: "Auction",
                EntityId: auctionId.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    auctionId = auctionId.Value,
                    newHighAmount = auction.Pricing.CurrentAmount,
                    minimumNextBid = auction.GetMinimumBidAmount().Amount,
                    currency = auction.Pricing.Currency.Id
                })),
            ct);
    }
}
