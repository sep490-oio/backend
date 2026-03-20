using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.NotificationContext.Enums;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class AuctionSubmittedEventHandler
    : INotificationHandler<AuctionSubmittedEvent>
{
    private readonly IDbContext _dbContext;
    private readonly ISender _sender;
    private readonly ILogger<AuctionSubmittedEventHandler> _logger;

    public AuctionSubmittedEventHandler(
        IDbContext dbContext,
        ISender sender,
        ILogger<AuctionSubmittedEventHandler> logger)
    {
        _dbContext = dbContext;
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(AuctionSubmittedEvent notification, CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(Guid.Parse(notification.AuctionId));

        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query
                .AsNoTracking()
                .Include(a => a.Item),
            cancellationToken: cancellationToken);

        if (auction is null)
        {
            _logger.LogWarning(
                "AuctionSubmittedEventHandler skipped because auction {AuctionId} was not found.",
                notification.AuctionId);
            return;
        }

        var isReadyForPublish = auction.Status == AuctionStatus.Approved || auction.Status == AuctionStatus.Scheduled;

        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: auction.Item.SellerId.Value,
                NotificationType: "auction",
                EventType: "auction_configuration_submitted",
                Title: "Cau hinh dau gia da duoc submit",
                Message: isReadyForPublish
                    ? $"Phien dau gia \"{auction.Item.Title.Value}\" da hoan tat cau hinh va san sang cho buoc publish."
                    : $"Phien dau gia \"{auction.Item.Title.Value}\" da duoc submit.",
                Priority: NotificationPriority.Normal,
                EntityType: "Auction",
                EntityId: auction.Id.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    auctionId = auction.Id.Value,
                    itemId = auction.ItemId.Value,
                    status = auction.Status.Id,
                    verifyByPlatform = notification.VerifyByPlatform
                })),
            cancellationToken);

        _logger.LogInformation(
            "Auction submitted. Id={AuctionId}, Item={ItemId}, Seller={SellerId}, VerifyByPlatform={VerifyByPlatform}, Status={Status}.",
            notification.AuctionId,
            notification.ItemId,
            notification.SellerId,
            notification.VerifyByPlatform,
            auction.Status.Id);
    }
}
