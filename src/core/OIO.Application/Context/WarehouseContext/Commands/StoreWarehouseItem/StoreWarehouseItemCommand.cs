using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.StoreWarehouseItem;

public sealed record StoreWarehouseItemCommand(
    Guid WarehouseItemId,
    Guid StorageLocationId
) : ICommand<WarehouseItemDto>;

internal sealed class StoreWarehouseItemCommandHandler(
    IDbContext db,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<StoreWarehouseItemCommandHandler> logger)
    : ICommandHandler<StoreWarehouseItemCommand, WarehouseItemDto>
{
    public async Task<Result<WarehouseItemDto, Error>> Handle(
        StoreWarehouseItemCommand request,
        CancellationToken cancellationToken)
    {
        var warehouseItemId   = WarehouseItemId.From(request.WarehouseItemId);
        var storageLocationId = WarehouseStorageLocationId.From(request.StorageLocationId);

        // ── 1. Load WarehouseItem ─────────────────────────────────────────────
        var warehouseItem = await db.Set<WarehouseItem>()
            .FirstOrDefaultAsync(w => w.Id == warehouseItemId, cancellationToken);

        if (warehouseItem is null)
            return WarehouseErrors.WarehouseItem.NotFound(request.WarehouseItemId.ToString());

        // ── 2. Load StorageLocation ───────────────────────────────────────────
        var location = await db.Set<WarehouseStorageLocation>()
            .FirstOrDefaultAsync(l => l.Id == storageLocationId, cancellationToken);

        if (location is null)
            return WarehouseErrors.StorageLocation.NotFound(request.StorageLocationId.ToString());

        if (location.IsOccupied)
            return WarehouseErrors.StorageLocation.Occupied;

        var now = clock.UtcNow;

        // ── 3. Assign location on the WarehouseItem ───────────────────────────
        var storeResult = warehouseItem.Store(storageLocationId, location.Label, now);
        if (storeResult.IsFailure)
            return storeResult.Error;

        // ── 4. Mark the physical shelf as occupied ────────────────────────────
        location.MarkOccupied();

        // ── 5. Complete the InboundShipment lifecycle (Inspected → Completed) ─
        var shipment = await db.Set<InboundShipment>()
            .FirstOrDefaultAsync(s => s.Id == warehouseItem.InboundShipmentId, cancellationToken);

        if (shipment is not null)
        {
            var completeResult = shipment.Complete(now);
            if (completeResult.IsFailure)
                return completeResult.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "WarehouseItem {WarehouseItemId} stored at location {LocationLabel} (shipment {ShipmentId} completed).",
            warehouseItem.Id.Value, location.Label, warehouseItem.InboundShipmentId.Value);

        return warehouseItem.ToDto();
    }
}