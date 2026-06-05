using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseToSellerShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.WarehouseContext.Commands.RequestWarehouseReinspection;

/// <summary>
/// Seller-initiated request to re-inspect a warehouse-rejected item that is
/// still physically at the warehouse (i.e. no return shipment has been
/// dispatched yet). This bypasses the online review flow guarded by
/// <c>ResubmitItemCommandHandler</c> and instead resets the existing
/// <see cref="WarehouseInspection"/> back into the inspector queue.
/// </summary>
public sealed record RequestWarehouseReinspectionCommand(
    Guid WarehouseItemId,
    string? Reason)
    : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return RequestWarehouseReinspectionCommand.Check()
            .WithOwnerName("RequestWarehouseReinspection")
            .Field(WarehouseItemId)
            .NotEmptyGuid();
    }
}

internal sealed class RequestWarehouseReinspectionCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    ILogger<RequestWarehouseReinspectionCommandHandler> logger)
    : ICommandHandler<RequestWarehouseReinspectionCommand>
{
    public async Task<UnitResult<Error>> Handle(
        RequestWarehouseReinspectionCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = clock.UtcNow;
        var warehouseItemId = WarehouseItemId.From(request.WarehouseItemId);

        // 1. Load WarehouseItem (and its parent inbound shipment for seller ownership).
        var warehouseItem = await dbContext.Set<WarehouseItem>()
            .FirstOrDefaultAsync(wh => wh.Id == warehouseItemId, cancellationToken);

        if (warehouseItem is null)
            return WarehouseErrors.WarehouseItem.NotFound(request.WarehouseItemId.ToString());

        var inboundShipment = await dbContext
            .Set<Domain.Context.WarehouseContext.Aggregates.InboundShipments.InboundShipment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == warehouseItem.InboundShipmentId, cancellationToken);

        if (inboundShipment is null)
            return WarehouseErrors.InboundShipment.NotFound(warehouseItem.InboundShipmentId.ToString());

        if (inboundShipment.SellerId != currentUser.UserId)
            return Error.Forbidden(
                "WarehouseItem.NotOwner",
                "Only the seller who owns this warehouse item can request a re-inspection.");

        // 2. Load Item, verify it is platform-verified and currently rejected.
        var itemId = ItemId.From(warehouseItem.ItemId);
        var item = await dbContext.GetByIdAsync<Item, ItemId>(
            id: itemId,
            queryBuilder: q => q.Include(i => i.ModerationReviews),
            cancellationToken: cancellationToken);

        if (item is null)
            return Error.NotFound("Item.NotFound", $"Item '{warehouseItem.ItemId}' was not found.");

        if (item.SellerId != currentUser.UserId)
            return Error.Forbidden(
                "Item.NotOwner",
                "Only the seller of the item can request a warehouse re-inspection.");

        if (item.Status != ItemStatus.Rejected)
            return Error.Conflict(
                "Item.NotRejected",
                $"Item is in '{item.Status.Id}' state — only rejected items can be re-inspected.");

        if (!item.RequiresPlatformInspection)
            return Error.Conflict(
                "Item.NotPlatformVerified",
                "Re-inspection is only valid for platform-verified items.");

        // 3. Load the inspection record. There is exactly one per warehouse item
        //    (unique index `idx_unique_warehouse_inspections_warehouse_item_id`).
        var inspection = await dbContext.Set<WarehouseInspection>()
            .Include(wi => wi.DecisionLogs)
            .FirstOrDefaultAsync(wi => wi.WarehouseItemId == warehouseItemId, cancellationToken);

        if (inspection is null)
            return WarehouseErrors.Inspection.NotFound(warehouseItemId.Value.ToString());

        if (inspection.DecisionStatus != WarehouseInspectionDecisionStatus.Rejected)
            return Error.Conflict(
                "WarehouseInspection.NotRejected",
                $"Inspection is in '{inspection.DecisionStatus.Id}' state — only rejected inspections can be re-inspected.");

        // 4. Block re-inspection if a return shipment is already in flight or delivered.
        //    A shipment that has been returned-to-warehouse or closed is fine — the goods
        //    are once again available physically, OR conceptually back to seller; either
        //    way the seller should open a new inbound shipment instead.
        var returnShipment = await dbContext.Set<WarehouseToSellerShipment>()
            .Where(s => s.WarehouseItemId == warehouseItemId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (returnShipment is not null
            && (returnShipment.Status == WarehouseToSellerShipmentStatus.InTransit
                || returnShipment.Status == WarehouseToSellerShipmentStatus.Delivered
                || returnShipment.Status == WarehouseToSellerShipmentStatus.Closed))
        {
            return Error.Conflict(
                "WarehouseItem.ReturnInProgress",
                "Hàng đang được trả về seller hoặc đã giao xong — không thể yêu cầu kiểm định lại tại kho.");
        }

        // 5. Apply the state transitions on both aggregates.
        var itemResult = item.RequestWarehouseReinspection(nowUtc);
        if (itemResult.IsFailure)
            return itemResult.Error;

        var inspectionResult = inspection.RequestReinspectionBySeller(
            sellerId: currentUser.UserId,
            reason: string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            now: nowUtc);
        if (inspectionResult.IsFailure)
            return inspectionResult.Error;

        // Cancel the pending shipment if any
        if (returnShipment is not null && returnShipment.Status == WarehouseToSellerShipmentStatus.Pending)
        {
            var cancelShipmentResult = returnShipment.Cancel(nowUtc);
            if (cancelShipmentResult.IsFailure)
                return cancelShipmentResult.Error;
        }

        // Reset the warehouse item back to Stored
        var undoReturnResult = warehouseItem.UndoReturnToSeller(nowUtc);
        if (undoReturnResult.IsFailure)
            return undoReturnResult.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Warehouse re-inspection requested by seller {SellerId} for warehouse item {WarehouseItemId} (item {ItemId}).",
            currentUser.UserId.Value, warehouseItemId.Value, item.Id.Value);

        return UnitResult.Success<Error>();
    }
}
