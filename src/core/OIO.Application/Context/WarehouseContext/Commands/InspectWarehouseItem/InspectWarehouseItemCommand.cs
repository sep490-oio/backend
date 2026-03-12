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
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using e = OIO.Domain.SeedWork.Errors.Error;

namespace OIO.Application.Context.WarehouseContext.Commands.InspectWarehouseItem;

public sealed record InspectWarehouseItemCommand(
    Guid InboundShipmentId,
    string ConditionId,
    string? InspectionNotes,
    List<string>? ImageUrls
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

        var conditionMaybe = WarehouseItemCondition.FromId(request.ConditionId);
        if (conditionMaybe.HasNoValue)
            return Error.Conflict("Warehouse.InvalidCondition", $"Unknown condition: '{request.ConditionId}'.");

        var now     = clock.UtcNow;
        var staffId = currentUser.UserId;
        var images  = request.ImageUrls is { Count: > 0 }
            ? InspectionImages.From(System.Text.Json.JsonSerializer.Serialize(request.ImageUrls))
            : InspectionImages.Empty;

        // Create + advance through Pending → Received → Inspected in one transaction
        var warehouseItem = WarehouseItem.Create(
            itemId:             shipment.ItemId,
            inboundShipmentId:  shipmentId,
            conditionOnArrival: conditionMaybe.Value,
            now:                now,
            inspectionNotes:    request.InspectionNotes,
            inspectionImages:   images);

        warehouseItem.MarkReceived(now);

        var inspectItemResult = warehouseItem.CompleteInspection(
            condition:        conditionMaybe.Value,
            inspectedBy:      staffId,
            now:              now,
            inspectionNotes:  request.InspectionNotes,
            inspectionImages: images);

        if (inspectItemResult.IsFailure)
            return inspectItemResult.Error;

        // Advance shipment: Arrived → Inspected
        var inspectShipmentResult = shipment.RecordInspected(staffId, now);
        if (inspectShipmentResult.IsFailure)
            return inspectShipmentResult.Error;

        db.Set<WarehouseItem>().Add(warehouseItem);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "WarehouseItem {WarehouseItemId} created for InboundShipment {ShipmentId} by staff {StaffId}.",
            warehouseItem.Id.Value, shipment.Id.Value, staffId);

        return warehouseItem.ToDto();
    }
}