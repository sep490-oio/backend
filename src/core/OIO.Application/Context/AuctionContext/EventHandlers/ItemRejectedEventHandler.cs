using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class ItemRejectedEventHandler
    : INotificationHandler<AuctionRejectedEvent>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ItemRejectedEventHandler> _logger;

    public ItemRejectedEventHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ILogger<ItemRejectedEventHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(AuctionRejectedEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "Item rejected. ItemId={ItemId}, AuctionId={AuctionId}, Reviewer={ReviewerId}, Reason={Reason}",
            notification.ItemId, notification.AuctionId, notification.ReviewerId, notification.Reason);

        var auctionId = AuctionId.From(Guid.Parse(notification.AuctionId));
        var reviewerId = UserId.From(Guid.Parse(notification.ReviewerId));

        // Find the auction to get seller ID
        var auction = await _dbContext.Set<Auction>()
            .Include(a => a.Item)
            .FirstOrDefaultAsync(a => a.Id == auctionId, ct);

        if (auction is null)
        {
            _logger.LogWarning("Auction {AuctionId} not found for item rejection dispute", notification.AuctionId);
            return;
        }

        // Only create dispute for platform verify flow
        if (!auction.VerifyByPlatform)
            return;

        // System-driven rejection: bypasses user-facing eligibility matrix because the
        // reviewer (admin) acts on behalf of the platform — no user role applies.
#pragma warning disable CS0618 // Type or member is obsolete
        var createResult = Dispute.Create(
            auctionId: auctionId,
            complainantId: reviewerId,
            respondentId: auction.Item.SellerId,
            type: DisputeType.ItemNotAsDescribed,
            title: $"Item rejected: {auction.Item.Title.Value}",
            description: $"Item was rejected during platform verification. Reason: {notification.Reason}",
            nowUtc: notification.OccurredAt,
            priority: DisputePriority.Medium);
#pragma warning restore CS0618

        if (createResult.IsFailure)
        {
            _logger.LogError("Failed to create dispute for rejected item {ItemId}: {Error}",
                notification.ItemId, createResult.Error);
            return;
        }

        var dispute = createResult.Value;
        var participantStates = new List<DisputeParticipantState>
        {
            DisputeParticipantState.Create(dispute.Id, reviewerId, notification.OccurredAt),
            DisputeParticipantState.Create(dispute.Id, auction.Item.SellerId, notification.OccurredAt)
        };

        _dbContext.Insert(dispute);
        _dbContext.InsertRange(participantStates);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Auto-created dispute {DisputeId} for rejected item {ItemId}",
            dispute.Id, notification.ItemId);
    }
}
