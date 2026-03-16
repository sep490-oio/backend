using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.AuctionContext;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
using OIO.Domain.Context.NotificationContext.Enums;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class AuctionCreatedEventHandler
    : INotificationHandler<AuctionCreatedEvent>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ISender _sender;
    private readonly ILogger<AuctionCreatedEventHandler> _logger;

    public AuctionCreatedEventHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ISender sender,
        ILogger<AuctionCreatedEventHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(AuctionCreatedEvent notification, CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;
        var auctionId = AuctionId.From(Guid.Parse(notification.AuctionId));
        var itemId = ItemId.From(Guid.Parse(notification.ItemId));
        var sellerId = Guid.Parse(notification.SellerId);

        var item = await _dbContext.GetByIdAsync<Item, ItemId>(
            id: itemId,
            queryBuilder: query => query.Include(i => i.ModerationReviews),
            cancellationToken: cancellationToken);

        if (item is null)
        {
            _logger.LogWarning(
                "Skipped auction created follow-up because item {ItemId} for auction {AuctionId} was not found.",
                notification.ItemId,
                notification.AuctionId);
            return;
        }

        var assignedAdminId = item.AssignedAdminId;
        if (assignedAdminId is null)
        {
            assignedAdminId = await AuctionReviewAssignments.ResolveReviewerIdAsync(
                _dbContext,
                cancellationToken);

            if (assignedAdminId is not null)
            {
                var assignResult = item.AssignAdmin(assignedAdminId.Value, nowUtc);
                if (assignResult.IsFailure)
                {
                    _logger.LogWarning(
                        "Failed to auto-assign reviewer for item {ItemId}. Error={Error}",
                        item.Id.Value,
                        assignResult.Error.Message);
                }
                else
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
            else
            {
                _logger.LogWarning(
                    "No active admin reviewer available for auction {AuctionId} / item {ItemId}.",
                    auctionId.Value,
                    item.Id.Value);
            }
        }

        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: sellerId,
                NotificationType: "auction",
                EventType: "auction_pending_publish",
                Title: "Phien dau gia da duoc tao",
                Message: $"Phien dau gia \"{item.Title.Value}\" da duoc tao va dang cho xuat ban.",
                Priority: NotificationPriority.Normal,
                EntityType: "Auction",
                EntityId: auctionId.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    auctionId = auctionId.Value,
                    itemId = item.Id.Value,
                    assignedAdminId = assignedAdminId?.Value
                })),
            cancellationToken);

        _logger.LogInformation(
            "Auction created. Id={AuctionId}, Item={ItemId}, Seller={SellerId}, AssignedAdmin={AssignedAdminId}.",
            auctionId.Value,
            item.Id.Value,
            sellerId,
            assignedAdminId?.Value);
    }
}
