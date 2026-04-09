using System.Text.Json;
using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Media;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Application.Context.MediaContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.Errors;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using e = OIO.Domain.SeedWork.Errors.Error;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.WarehouseContext.Commands.InspectWarehouseItem;

/// <summary>
/// Inspection command — staff submits up to 4 named photos and optional notes.
/// All photo slots are optional individually; at least one is recommended.
/// </summary>
public sealed record InspectWarehouseItemCommand(
    Guid InboundShipmentId,
    string Condition,
    string? InspectionNotes,
    IReadOnlyList<Guid> InspectionMediaUploadIds
) : ICommand<WarehouseInspectionDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return InspectWarehouseItemCommand.Check()
            .WithOwnerName("InspectWarehouseItem")
            .Field(InboundShipmentId)
            .NotEmptyGuid()
            .Field(Condition)
            .NotWhiteSpace()
            .InSet(WarehouseItemCondition.All.Select(c => c.Id));
    }
}

internal sealed class InspectWarehouseItemCommandHandler(
    IDbContext db,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    UploadContextRegistry contextRegistry,
    IMediaRelocationService mediaRelocationService,
    ISender sender,
    ILogger<InspectWarehouseItemCommandHandler> logger)
    : ICommandHandler<InspectWarehouseItemCommand, WarehouseInspectionDto>
{
    public async Task<Result<WarehouseInspectionDto, e>> Handle(
        InspectWarehouseItemCommand request,
        CancellationToken cancellationToken)
    {
        if (request.InspectionMediaUploadIds.Count == 0)
            return WarehouseErrors.Inspection.EvidenceRequired;

        var shipmentId = InboundShipmentId.From(request.InboundShipmentId);

        var shipment = await db.Set<InboundShipment>()
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return WarehouseErrors.InboundShipment.NotFound(request.InboundShipmentId.ToString());

        var existingInspection = await db.Set<WarehouseInspection>()
            .FirstOrDefaultAsync(i => i.InboundShipmentId == shipmentId, cancellationToken);

        if (existingInspection is not null)
            return WarehouseErrors.Inspection.AlreadyExists;

        var itemId = ItemId.From(shipment.ItemId);
        var item = await db.GetByIdAsync<Item, ItemId>(
            itemId,
            cancellationToken: cancellationToken);

        if (item is null)
            return Error.NotFound("Item.NotFound", $"Item '{shipment.ItemId}' was not found.");

        var conditionMaybe = WarehouseItemCondition.FromId(request.Condition);
        if (conditionMaybe.HasNoValue)
            return Error.Conflict("Warehouse.InvalidCondition", $"Unknown condition: '{request.Condition}'.");

        var mediaUploadIds = request.InspectionMediaUploadIds
            .Select(MediaUploadId.From)
            .ToList();

        var uploads = await db.Set<MediaUpload>()
            .Where(x => mediaUploadIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (uploads.Count != mediaUploadIds.Count)
        {
            var missingIds = mediaUploadIds
                .Where(id => uploads.All(upload => upload.Id != id))
                .Select(id => id.Value.ToString());
            return MediaErrors.NotFounds(string.Join(", ", missingIds));
        }

        if (uploads.Any(x => x.UserId != currentUser.UserId))
            return MediaErrors.NotOwnedByUser(string.Join(", ", uploads.Where(x => x.UserId != currentUser.UserId).Select(x => x.Id.Value)));

        if (uploads.Any(x => !x.IsConfirmed))
            return MediaErrors.NotConfirm;

        if (uploads.Any(x => !contextRegistry.IsWarehouseInspectionContext(x.Context)))
            return MediaErrors.WrongContext("warehouse inspection", contextRegistry.GetAllContext());

        if (uploads.Any(x => x.IsLinked))
            return MediaErrors.AlreadyLinked;

        var now = clock.UtcNow;
        var staffId = currentUser.UserId;

        var warehouseItem = await db.Set<WarehouseItem>()
            .FirstOrDefaultAsync(w => w.InboundShipmentId == shipmentId, cancellationToken);

        if (warehouseItem is null)
        {
            warehouseItem = WarehouseItem.Create(
                itemId: shipment.ItemId,
                inboundShipmentId: shipmentId,
                now: now);
            db.Insert(warehouseItem);
        }

        if (warehouseItem.Status == WarehouseItemStatus.Pending)
            warehouseItem.MarkReceived(now);

        var inspectionResult = warehouseItem.MarkInspected(now);
        if (inspectionResult.IsFailure)
            return inspectionResult.Error;

        var evidence = InspectionEvidence.From(JsonSerializer.Serialize(
            uploads.Select(upload => InspectionEvidenceSnapshot.Create(upload.StorageRef, upload.Info)).ToList()));

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

        var inspectShipmentResult = shipment.RecordInspected(staffId, now);
        if (inspectShipmentResult.IsFailure)
            return inspectShipmentResult.Error;

        db.Insert(inspection);

        foreach (var upload in uploads)
        {
            var linkResult = upload.LinkToEntity(inspection.Id, now);
            if (linkResult.IsFailure)
                return linkResult.Error;

            await mediaRelocationService.RelocateLinkedUploadAsync(upload, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var auctionId = await db.Set<Auction>()
            .Where(x => x.ItemId == itemId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => (Guid?)x.Id.Value)
            .FirstOrDefaultAsync(cancellationToken);

        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: item.SellerId.Value,
                NotificationType: "moderation",
                EventType: "platform_inspection_recorded",
                Title: "San pham da duoc kiem dinh",
                Message: $"San pham \"{item.Title.Value}\" da duoc kiem dinh va dang cho ket luan tu inspector.",
                Priority: NotificationPriority.Normal,
                EntityType: auctionId.HasValue ? "Auction" : "Item",
                EntityId: auctionId ?? item.Id.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    inspectionId = inspection.Id.Value,
                    warehouseItemId = warehouseItem.Id.Value,
                    inboundShipmentId = shipment.Id.Value,
                    itemId = item.Id.Value,
                    auctionId
                })),
            cancellationToken);

        logger.LogInformation(
            "Warehouse inspection {InspectionId} created for inbound shipment {ShipmentId} by {InspectorId}.",
            inspection.Id.Value,
            shipment.Id.Value,
            staffId.Value);

        return inspection.ToDto();
    }
}

