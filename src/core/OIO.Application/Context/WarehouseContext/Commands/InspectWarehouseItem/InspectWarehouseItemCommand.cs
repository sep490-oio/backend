using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using e = OIO.Domain.SeedWork.Errors.Error;

namespace OIO.Application.Context.WarehouseContext.Commands.InspectWarehouseItem;

/// <summary>
/// Inspection command — staff submits up to 4 named photos and optional notes.
/// All photo slots are optional individually; at least one is recommended.
/// </summary>
public sealed record InspectWarehouseItemCommand(
    Guid    InboundShipmentId,
    string? InspectionNotes,
    /// <summary>Photo of the front / exterior of the package.</summary>
    Guid? FrontPhotoUploadId,
    /// <summary>Photo of the shipping label so the tracking code is on record.</summary>
    Guid? ShippingLabelUploadId,
    /// <summary>Photo of the seal / tape showing whether the package was tampered with.</summary>
    Guid? SealConditionUploadId,
    /// <summary>Photo of the contents inside the package once opened.</summary>
    Guid? InsideContentsUploadId
) : ICommand<WarehouseItemDto>;

internal sealed class InspectWarehouseItemCommandHandler(
    IDbContext db,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    ILogger<InspectWarehouseItemCommandHandler> logger)
    : ICommandHandler<InspectWarehouseItemCommand, WarehouseItemDto>
{
    public async Task<Result<WarehouseItemDto, e>> Handle(
        InspectWarehouseItemCommand request,
        CancellationToken cancellationToken)
    {
        var shipmentId = InboundShipmentId.From(request.InboundShipmentId);

        var shipment = await db.Set<InboundShipment>()
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return WarehouseErrors.InboundShipment.NotFound(request.InboundShipmentId.ToString());

        if (shipment.Status != InboundShipmentStatus.Arrived)
            return WarehouseErrors.InboundShipment.CannotInspect;

        var alreadyExists = await db.Set<WarehouseItem>()
            .AnyAsync(w => w.InboundShipmentId == shipmentId, cancellationToken);

        if (alreadyExists)
            return WarehouseErrors.WarehouseItem.AlreadyInspected;

        var now     = clock.UtcNow;
        var staffId = currentUser.UserId;

        // Named slots in fixed sort order — Front photo (slot 0) is always the primary image.
        var photoSlots = new[]
        {
            (UploadId: request.FrontPhotoUploadId,     SortOrder: 0, IsPrimary: true),
            (UploadId: request.ShippingLabelUploadId,  SortOrder: 1, IsPrimary: false),
            (UploadId: request.SealConditionUploadId,  SortOrder: 2, IsPrimary: false),
            (UploadId: request.InsideContentsUploadId, SortOrder: 3, IsPrimary: false),
        };

        var requestedIds = photoSlots
            .Where(s => s.UploadId.HasValue)
            .Select(s => MediaUploadId.From(s.UploadId!.Value))
            .ToList();

        // Fetch and validate all provided upload IDs in one query
        var uploads = new List<MediaUpload>();
        if (requestedIds.Count > 0)
        {
            uploads = await db.Set<MediaUpload>()
                .Where(u => requestedIds.Contains(u.Id) && u.IsConfirmed && !u.IsLinked && u.UserId == staffId)
                .ToListAsync(cancellationToken);

            if (uploads.Count != requestedIds.Count)
                return e.Validation("Inspection", "Warehouse.InvalidMedia",
                    "One or more photo uploads are invalid, unconfirmed, or do not belong to you.");
        }

        // Condition is not set by staff — use Good as a neutral default.
        var defaultCondition = WarehouseItemCondition.Good;

        // Create + advance: Pending → Received → Inspected in one transaction
        var warehouseItem = WarehouseItem.Create(
            itemId:             shipment.ItemId,
            inboundShipmentId:  shipmentId,
            conditionOnArrival: defaultCondition,
            now:                now,
            inspectionNotes:    request.InspectionNotes);

        warehouseItem.MarkReceived(now);

        var inspectResult = warehouseItem.CompleteInspection(
            condition:       defaultCondition,
            inspectedBy:     staffId,
            now:             now,
            inspectionNotes: request.InspectionNotes);

        if (inspectResult.IsFailure)
            return inspectResult.Error;

        // Attach photos in named-slot order
        foreach (var slot in photoSlots.Where(s => s.UploadId.HasValue))
        {
            var upload = uploads.First(u => u.Id.Value == slot.UploadId!.Value);
            warehouseItem.AddMedia(
                nowUtc:     now,
                upload:     upload,
                isPrimary:  slot.IsPrimary,
                maxForType: 4,
                sortOrder:  slot.SortOrder);
            var linkResult = upload.LinkToEntity(warehouseItem.Id.Value, nowUtc: now);
            if (linkResult.IsFailure) return linkResult.Error;
        }

        // Advance shipment: Arrived → Inspected
        var shipmentResult = shipment.RecordInspected(staffId, now);
        if (shipmentResult.IsFailure)
            return shipmentResult.Error;

        db.Set<WarehouseItem>().Add(warehouseItem);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "WarehouseItem {WarehouseItemId} created for InboundShipment {ShipmentId} by staff {StaffId}.",
            warehouseItem.Id.Value, shipment.Id.Value, staffId);

        return warehouseItem.ToDto();
    }
}