using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.NotificationContext.Enums;

namespace OIO.Application.Context.NotificationContext.EventHandlers;

internal sealed class AuctionApprovedNotificationHandler
    : INotificationHandler<AuctionApprovedEvent>
{
    private readonly IDbContext _dbContext;
    private readonly ISender _sender;
    private readonly ILogger<AuctionApprovedNotificationHandler> _logger;

    public AuctionApprovedNotificationHandler(
        IDbContext dbContext,
        ISender sender,
        ILogger<AuctionApprovedNotificationHandler> logger)
    {
        _dbContext = dbContext;
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(AuctionApprovedEvent notification, CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(Guid.Parse(notification.AuctionId));
        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            auctionId,
            queryBuilder: query => query
                .AsNoTracking()
                .Include(a => a.Item),
            cancellationToken: cancellationToken);

        if (auction is null)
        {
            _logger.LogWarning(
                "Skipped approval notification because auction {AuctionId} was not found.",
                notification.AuctionId);
            return;
        }

        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: auction.Item.SellerId.Value,
                NotificationType: "moderation",
                EventType: "item_approved",
                Title: "San pham da duoc phe duyet",
                Message: $"San pham \"{auction.Item.Title.Value}\" da duoc phe duyet va san sang cho buoc tiep theo.",
                Priority: NotificationPriority.Normal,
                EntityType: "Auction",
                EntityId: auction.Id.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    auctionId = auction.Id.Value,
                    itemId = auction.ItemId.Value,
                    reviewerId = notification.ReviewerId
                })),
            cancellationToken);
    }
}

internal sealed class AuctionRejectedNotificationHandler
    : INotificationHandler<AuctionRejectedEvent>
{
    private readonly IDbContext _dbContext;
    private readonly ISender _sender;
    private readonly ILogger<AuctionRejectedNotificationHandler> _logger;

    public AuctionRejectedNotificationHandler(
        IDbContext dbContext,
        ISender sender,
        ILogger<AuctionRejectedNotificationHandler> logger)
    {
        _dbContext = dbContext;
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(AuctionRejectedEvent notification, CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(Guid.Parse(notification.AuctionId));
        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            auctionId,
            queryBuilder: query => query
                .AsNoTracking()
                .Include(a => a.Item),
            cancellationToken: cancellationToken);

        if (auction is null)
        {
            _logger.LogWarning(
                "Skipped rejection notification because auction {AuctionId} was not found.",
                notification.AuctionId);
            return;
        }

        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: auction.Item.SellerId.Value,
                NotificationType: "moderation",
                EventType: "item_rejected",
                Title: "San pham can chinh sua",
                Message: $"San pham \"{auction.Item.Title.Value}\" chua duoc phe duyet. Ly do: {notification.Reason}",
                Priority: NotificationPriority.High,
                EntityType: "Auction",
                EntityId: auction.Id.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    auctionId = auction.Id.Value,
                    itemId = auction.ItemId.Value,
                    reviewerId = notification.ReviewerId,
                    reason = notification.Reason,
                    rejectionCount = notification.RejectionCount
                })),
            cancellationToken);
    }
}
