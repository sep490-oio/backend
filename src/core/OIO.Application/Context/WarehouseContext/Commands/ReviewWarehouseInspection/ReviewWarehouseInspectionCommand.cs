using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Application.Context.WarehouseContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using e = OIO.Domain.SeedWork.Errors.Error;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.WarehouseContext.Commands.ReviewWarehouseInspection;

public sealed record ReviewWarehouseInspectionCommand(
    Guid InboundShipmentId,
    string Decision,
    string? Reason = null) : ICommand<WarehouseInspectionDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return ReviewWarehouseInspectionCommand.Check()
            .WithOwnerName("ReviewWarehouseInspection")
            .Field(InboundShipmentId)
            .NotEmptyGuid()
            .Field(Decision)
            .NotWhiteSpace()
            .InSet(["approve", "reject"]);
    }
}

internal sealed class ReviewWarehouseInspectionCommandHandler(
    IDbContext db,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    ContinueVerifiedAuctionService continuationService,
    IWarehouseReturnShipmentFactory returnShipmentFactory,
    ISender sender,
    ILogger<ReviewWarehouseInspectionCommandHandler> logger)
    : ICommandHandler<ReviewWarehouseInspectionCommand, WarehouseInspectionDto>
{
    public async Task<Result<WarehouseInspectionDto, e>> Handle(
        ReviewWarehouseInspectionCommand request,
        CancellationToken cancellationToken)
    {
        var shipmentId = InboundShipmentId.From(request.InboundShipmentId);
        var inspection = await db.Set<WarehouseInspection>()
            .FirstOrDefaultAsync(x => x.InboundShipmentId == shipmentId, cancellationToken);

        if (inspection is null)
            return WarehouseErrors.Inspection.NotFound(request.InboundShipmentId.ToString());

        if (inspection.DecisionStatus != WarehouseInspectionDecisionStatus.PendingReview)
            return WarehouseErrors.Inspection.AlreadyReviewed;

        var itemId = ItemId.From(inspection.ItemId);
        var item = await db.GetByIdAsync<Item, ItemId>(
            itemId,
            queryBuilder: query => query
                .Include(x => x.Media)
                .Include(x => x.ModerationReviews)
                .Include(x => x.Auctions),
            cancellationToken: cancellationToken);

        if (item is null)
            return Error.NotFound("Item.NotFound", $"Item '{inspection.ItemId}' was not found.");

        if (item.Status != ItemStatus.PendingVerify)
            return Error.Conflict("WarehouseInspection.InvalidItemState", $"Item is in '{item.Status.Id}' and cannot be reviewed from platform inspection.");

        var now = clock.UtcNow;
        var reviewerId = currentUser.UserId;
        VerifiedAuctionContinuationResult? continuationResult = null;

        if (string.Equals(request.Decision, "reject", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                return Error.Validation("Reason", "WarehouseInspection.ReasonRequired", "Reason is required when rejecting an inspection.");

            var rejectInspectionResult = inspection.Reject(reviewerId, request.Reason.Trim(), now);
            if (rejectInspectionResult.IsFailure)
                return rejectInspectionResult.Error;

            var rejectItemResult = item.RejectFromPlatformInspection(reviewerId, request.Reason.Trim(), now);
            if (rejectItemResult.IsFailure)
                return rejectItemResult.Error;

            var returnShipmentResult = await returnShipmentFactory.EnsureShipmentExistsAsync(
                inspectionId:    inspection.Id,
                warehouseItemId: inspection.WarehouseItemId,
                rejectionReason: request.Reason.Trim(),
                nowUtc:          now,
                cancellationToken: cancellationToken,
                persistImmediately: false);

            if (!IsReturnShipmentReady(returnShipmentResult.Outcome))
            {
                logger.LogWarning(
                    "ReviewWarehouseInspection reject could not create return shipment. Inspection {InspectionId}, WarehouseItem {WarehouseItemId}, Outcome {Outcome}, Error {Error}.",
                    inspection.Id.Value,
                    inspection.WarehouseItemId.Value,
                    returnShipmentResult.Outcome,
                    returnShipmentResult.Error?.Message);

                return returnShipmentResult.Error
                    ?? Error.Conflict(
                        "WarehouseInspection.ReturnShipmentNotCreated",
                        $"Could not create warehouse return shipment for rejected inspection '{inspection.Id.Value}'.");
            }
        }
        else
        {
            var mappedCondition = ItemCondition.All.FirstOrDefault(x => x.Id == inspection.ConditionOnArrival.Id);
            if (mappedCondition is null)
                return WarehouseErrors.Inspection.UnsupportedApprovalCondition;

            if (string.Equals(item.Condition.Id, mappedCondition.Id, StringComparison.Ordinal))
            {
                var approveInspectionResult = inspection.Approve(reviewerId, now);
                if (approveInspectionResult.IsFailure)
                    return approveInspectionResult.Error;

                var approveItemResult = item.ApproveFromPlatformInspection(reviewerId, now);
                if (approveItemResult.IsFailure)
                    return approveItemResult.Error;

                var continueResult = await continuationService.ContinueAsync(item.Id.Value, cancellationToken);
                if (continueResult.IsFailure)
                    return continueResult.Error;

                continuationResult = continueResult.Value;
            }
            else
            {
                var requestConfirmationResult = inspection.RequireConditionConfirmation(reviewerId, now);
                if (requestConfirmationResult.IsFailure)
                    return requestConfirmationResult.Error;

                var itemConfirmationResult = item.RequireConditionConfirmation(reviewerId, now);
                if (itemConfirmationResult.IsFailure)
                    return itemConfirmationResult.Error;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var auctionId = item.Auctions
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => (Guid?)x.Id.Value)
            .FirstOrDefault();

        if (inspection.DecisionStatus == WarehouseInspectionDecisionStatus.Rejected)
        {
            await NotificationDispatch.DispatchAsync(
                sender,
                logger,
                new CreateNotificationCommand(
                    UserId: item.SellerId.Value,
                    NotificationType: "moderation",
                    EventType: "platform_verification_rejected",
                    Title: "San pham bi tu choi sau kiem dinh",
                    Message: $"San pham \"{item.Title.Value}\" bi tu choi sau kiem dinh. Ly do: {inspection.DecisionReason}.",
                    Priority: NotificationPriority.High,
                    EntityType: auctionId.HasValue ? "Auction" : "Item",
                    EntityId: auctionId ?? item.Id.Value,
                    Metadata: NotificationDispatch.SerializeMetadata(new
                    {
                        inspectionId = inspection.Id.Value,
                        itemId = item.Id.Value,
                        auctionId,
                        decision = inspection.DecisionStatus.Id
                    })),
                cancellationToken);
        }
        else if (inspection.DecisionStatus == WarehouseInspectionDecisionStatus.ConditionConfirmationRequired)
        {
            await NotificationDispatch.DispatchAsync(
                sender,
                logger,
                new CreateNotificationCommand(
                    UserId: item.SellerId.Value,
                    NotificationType: "moderation",
                    EventType: "inspected_condition_confirmation_required",
                    Title: "Can xac nhan tinh trang sau kiem dinh",
                    Message:
                        $"San pham \"{item.Title.Value}\" da duoc chap nhan ve mat kiem dinh, " +
                        $"nhung tinh trang thuc te la \"{inspection.ConditionOnArrival.Id}\". Vui long xac nhan de tiep tuc dau gia.",
                    Priority: NotificationPriority.High,
                    EntityType: auctionId.HasValue ? "Auction" : "Item",
                    EntityId: auctionId ?? item.Id.Value,
                    Metadata: NotificationDispatch.SerializeMetadata(new
                    {
                        inspectionId = inspection.Id.Value,
                        itemId = item.Id.Value,
                        auctionId,
                        decision = inspection.DecisionStatus.Id
                    })),
                cancellationToken);
        }
        else
        {
            var shouldCreateAuctionNext = !auctionId.HasValue;

            await NotificationDispatch.DispatchAsync(
                sender,
                logger,
                new CreateNotificationCommand(
                    UserId: item.SellerId.Value,
                    NotificationType: "moderation",
                    EventType: continuationResult?.Continued == true
                        ? "auction_auto_continued_after_verification"
                        : shouldCreateAuctionNext
                            ? "platform_verification_approved_create_auction_next"
                            : "platform_verification_approved",
                    Title: continuationResult?.Continued == true
                        ? "Dau gia duoc tiep tuc sau kiem dinh"
                        : shouldCreateAuctionNext
                            ? "San pham da qua kiem dinh, hay tao dau gia"
                            : "San pham da duoc chap nhan sau kiem dinh",
                    Message: continuationResult?.Continued == true
                        ? $"San pham \"{item.Title.Value}\" da duoc chap nhan va phien dau gia da chuyen sang trang thai \"{continuationResult.AuctionStatus}\"."
                        : shouldCreateAuctionNext
                            ? $"San pham \"{item.Title.Value}\" da qua kiem dinh. Vui long tao phien dau gia bang POST /api/items/{item.Id.Value}/auctions."
                            : $"San pham \"{item.Title.Value}\" da duoc chap nhan sau kiem dinh.",
                    Priority: shouldCreateAuctionNext ? NotificationPriority.High : NotificationPriority.Normal,
                    EntityType: auctionId.HasValue ? "Auction" : "Item",
                    EntityId: auctionId ?? item.Id.Value,
                    Metadata: NotificationDispatch.SerializeMetadata(new
                    {
                        inspectionId = inspection.Id.Value,
                        itemId = item.Id.Value,
                        auctionId,
                        decision = inspection.DecisionStatus.Id,
                        continuation = continuationResult
                    }),
                    Actions: shouldCreateAuctionNext
                        ? NotificationDispatch.SerializeMetadata(new[]
                        {
                            new
                            {
                                type = "create_auction_from_item",
                                label = "Create auction",
                                method = "POST",
                                endpoint = $"api/items/{item.Id.Value}/auctions"
                            }
                        })
                        : null),
                cancellationToken);
        }

        return inspection.ToDto();
    }

    private static bool IsReturnShipmentReady(EnsureShipmentOutcome outcome) =>
        outcome is EnsureShipmentOutcome.Created
            or EnsureShipmentOutcome.AlreadyExists
            or EnsureShipmentOutcome.CreatedViaDbDedup;
}
