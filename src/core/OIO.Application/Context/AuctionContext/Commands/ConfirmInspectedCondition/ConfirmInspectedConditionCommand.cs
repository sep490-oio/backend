using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.AuctionContext.Commands.ConfirmInspectedCondition;

public sealed record ConfirmInspectedConditionCommand(Guid ItemId) : ICommand<ItemDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return ConfirmInspectedConditionCommand.Check()
            .WithOwnerName("ConfirmInspectedCondition")
            .Field(ItemId)
            .NotEmptyGuid();
    }
}

internal sealed class ConfirmInspectedConditionCommandHandler(
    IDbContext db,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    ContinueVerifiedAuctionService continuationService,
    ISender sender,
    ILogger<ConfirmInspectedConditionCommandHandler> logger)
    : ICommandHandler<ConfirmInspectedConditionCommand, ItemDto>
{
    public async Task<Result<ItemDto, Error>> Handle(
        ConfirmInspectedConditionCommand request,
        CancellationToken cancellationToken)
    {
        var itemId = ItemId.From(request.ItemId);
        var item = await db.GetByIdAsync<Item, ItemId>(
            itemId,
            queryBuilder: query => query
                .Include(x => x.Media)
                .Include(x => x.ModerationReviews)
                .Include(x => x.Auctions),
            cancellationToken: cancellationToken);

        if (item is null)
            return Error.NotFound("Item.NotFound", $"Item '{request.ItemId}' was not found.");

        if (item.SellerId != currentUser.UserId)
            return Error.Forbidden("Item.NotOwnedByUser", "Only the seller can confirm the inspected condition.");

        if (item.Status != ItemStatus.PendingConditionConfirmation)
            return Error.Conflict("Item.InvalidState", $"Item is in '{item.Status.Id}' and does not require condition confirmation.");

        var inspection = await db.Set<WarehouseInspection>()
            .Where(x =>
                x.ItemId == item.Id.Value &&
                x.DecisionStatus == WarehouseInspectionDecisionStatus.ConditionConfirmationRequired)
            .OrderByDescending(x => x.InspectedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (inspection is null)
            return WarehouseErrors.Inspection.ConditionConfirmationNotRequired;

        var mappedCondition = ItemCondition.All.FirstOrDefault(x => x.Id == inspection.ConditionOnArrival.Id);
        if (mappedCondition is null)
            return WarehouseErrors.Inspection.UnsupportedApprovalCondition;

        var now = clock.UtcNow;

        var confirmInspectionResult = inspection.ConfirmSellerCondition(now);
        if (confirmInspectionResult.IsFailure)
            return confirmInspectionResult.Error;

        var confirmItemResult = item.ConfirmInspectedCondition(currentUser.UserId, mappedCondition, now);
        if (confirmItemResult.IsFailure)
            return confirmItemResult.Error;

        var continuationResult = await continuationService.ContinueAsync(item.Id.Value, cancellationToken);
        if (continuationResult.IsFailure)
            return continuationResult.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var auctionId = item.Auctions
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => (Guid?)x.Id.Value)
            .FirstOrDefault();

        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: item.SellerId.Value,
                NotificationType: "moderation",
                EventType: continuationResult.Value.Continued
                    ? "auction_auto_continued_after_condition_confirmation"
                    : "inspected_condition_confirmed",
                Title: continuationResult.Value.Continued
                    ? "Da xac nhan tinh trang va tiep tuc dau gia"
                    : "Da xac nhan tinh trang sau kiem dinh",
                Message: continuationResult.Value.Continued
                    ? $"Ban da xac nhan tinh trang san pham \"{item.Title.Value}\" va phien dau gia da chuyen sang \"{continuationResult.Value.AuctionStatus}\"."
                    : $"Ban da xac nhan tinh trang san pham \"{item.Title.Value}\".",
                Priority: NotificationPriority.Normal,
                EntityType: auctionId.HasValue ? "Auction" : "Item",
                EntityId: auctionId ?? item.Id.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    inspectionId = inspection.Id.Value,
                    itemId = item.Id.Value,
                    auctionId,
                    continuation = continuationResult.Value
                })),
            cancellationToken);

        return item.ToDto();
    }
}
