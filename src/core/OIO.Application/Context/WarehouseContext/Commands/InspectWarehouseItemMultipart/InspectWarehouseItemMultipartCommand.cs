using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Media;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using System.Text.Json;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using e = OIO.Domain.SeedWork.Errors.Error;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.WarehouseContext.Commands.InspectWarehouseItemMultipart;

/// <summary>
/// Single-step inspection for warehouse staff on mobile.
/// Staff submits raw photos — the server uploads them to Cloudinary,
/// creates pre-confirmed MediaUpload records, and creates the WarehouseItem
/// all within one transaction.
///
/// Replaces the 3-step (request signature → upload → confirm → inspect) flow
/// with a single multipart POST.
/// </summary>
public sealed record InspectWarehouseItemMultipartCommand(
    Guid         InboundShipmentId,
    string       Condition,
    string?      InspectionNotes,
    /// <summary>Front / exterior of the package.</summary>
    IFormFile?   FrontPhoto,
    /// <summary>Shipping label / waybill.</summary>
    IFormFile?   ShippingLabelPhoto,
    /// <summary>Seal or tape condition.</summary>
    IFormFile?   SealConditionPhoto,
    /// <summary>Contents inside the box after opening.</summary>
    IFormFile?   InsideContentsPhoto
) : ICommand<WarehouseItemDto>;

internal sealed class InspectWarehouseItemMultipartCommandHandler(
    IDbContext db,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    IMediaDirectUploadService directUpload,
    ILogger<InspectWarehouseItemMultipartCommandHandler> logger)
    : ICommandHandler<InspectWarehouseItemMultipartCommand, WarehouseItemDto>
{
    private const string InspectionFolder = "warehouse/inspections";

    public async Task<Result<WarehouseItemDto, e>> Handle(
        InspectWarehouseItemMultipartCommand request,
        CancellationToken cancellationToken)
    {
        var shipmentId = InboundShipmentId.From(request.InboundShipmentId);

        // ── 1. Load + validate shipment ───────────────────────────────────────
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

        // ── 2. Upload photos to Cloudinary server-side ────────────────────────
        var slots = new[]
        {
            (File: request.FrontPhoto,          SortOrder: 0, IsPrimary: true),
            (File: request.ShippingLabelPhoto,  SortOrder: 1, IsPrimary: false),
            (File: request.SealConditionPhoto,  SortOrder: 2, IsPrimary: false),
            (File: request.InsideContentsPhoto, SortOrder: 3, IsPrimary: false),
        };

        var uploadedMedia = new List<(MediaUpload Upload, int SortOrder, bool IsPrimary)>();

        foreach (var slot in slots.Where(s => s.File is not null))
        {
            await using var stream = slot.File!.OpenReadStream();

            var uploadResult = await directUpload.UploadAsync(
                fileStream: stream,
                fileName:   slot.File.FileName,
                folder:     InspectionFolder,
                ct:         cancellationToken);

            if (uploadResult.IsFailure)
                return uploadResult.Error;

            var result = uploadResult.Value;

            var storageRefResult = StorageRef.Create(result.PublicId, InspectionFolder);
            if (storageRefResult.IsFailure) return storageRefResult.Error;

            var mediaInfo = MediaInfo.Create(
                secureUrl: result.SecureUrl,
                fileName:  result.FileName,
                bytes:     result.Bytes,
                format:    result.Format,
                width:     result.Width,
                height:    result.Height);

            // Pre-confirmed since server did the upload — no 3-step cycle needed
            var upload = MediaUpload.CreateServerSide(
                userId:       staffId,
                context:      "warehouse_inspection",
                resourceType: "image",
                mediaInfo:    mediaInfo,
                storageRef:   storageRefResult.Value,
                nowUtc:       now);

            uploadedMedia.Add((upload, slot.SortOrder, slot.IsPrimary));
        }

        // ── 3. Create WarehouseItem ───────────────────────────────────────────
        var warehouseItem = WarehouseItem.Create(
            itemId:             shipment.ItemId,
            inboundShipmentId:  shipmentId,
            now:                now);

        warehouseItem.MarkReceived(now);

        var inspectResult = warehouseItem.MarkInspected(now);

        if (inspectResult.IsFailure) return inspectResult.Error;

        // ── 4. Attach photos + link uploads ───────────────────────────────────
        foreach (var (upload, sortOrder, isPrimary) in uploadedMedia)
        {
            warehouseItem.AddMedia(
                nowUtc:     now,
                upload:     upload,
                isPrimary:  isPrimary,
                maxForType: 4,
                sortOrder:  sortOrder);

            var linkResult = upload.LinkToEntity(warehouseItem.Id, nowUtc: now);
            if (linkResult.IsFailure) return linkResult.Error;
        }

        // ── 5. Create WarehouseInspection record ─────────────────────────────
        var conditionMaybe = WarehouseItemCondition.FromId(request.Condition);
        if (conditionMaybe.HasNoValue)
            return e.Conflict("Warehouse.InvalidCondition", $"Unknown condition: '{request.Condition}'.");

        var itemId = ItemId.From(shipment.ItemId);
        var item = await db.Set<Item>()
            .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);

        if (item is null)
            return e.NotFound("Item.NotFound", $"Item '{shipment.ItemId}' was not found.");

        var evidence = InspectionEvidence.From(JsonSerializer.Serialize(
            uploadedMedia.Select(m => InspectionEvidenceSnapshot.Create(m.Upload.StorageRef, m.Upload.Info)).ToList()));

        var createInspectionResult = WarehouseInspection.Create(
            warehouseItemId: warehouseItem.Id,
            inboundShipmentId: shipmentId,
            itemId: shipment.ItemId,
            declaredCondition: item.Condition,
            conditionOnArrival: conditionMaybe.Value,
            evidence: evidence,
            inspectedBy: staffId,
            now: now,
            inspectionNotes: request.InspectionNotes);

        if (createInspectionResult.IsFailure)
            return createInspectionResult.Error;

        var inspection = createInspectionResult.Value;

        // ── 6. Advance shipment: Arrived → Inspected ──────────────────────────
        var shipmentResult = shipment.RecordInspected(staffId, now);
        if (shipmentResult.IsFailure) return shipmentResult.Error;

        // ── 7. Persist everything in one transaction ──────────────────────────
        // Insert MediaUpload records so they're tracked/auditable
        foreach (var (upload, _, _) in uploadedMedia)
            db.Set<MediaUpload>().Add(upload);

        db.Set<WarehouseItem>().Add(warehouseItem);
        db.Set<WarehouseInspection>().Add(inspection);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "WarehouseItem {ItemId} created for shipment {ShipmentId} with {PhotoCount} inspection photos.",
            warehouseItem.Id.Value, shipment.Id.Value, uploadedMedia.Count);

        return warehouseItem.ToDto();
    }
}