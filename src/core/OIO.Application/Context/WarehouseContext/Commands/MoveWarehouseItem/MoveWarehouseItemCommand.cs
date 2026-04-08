using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.MoveWarehouseItem;

public sealed record MoveWarehouseItemCommand(
    Guid WarehouseItemId,
    Guid NewLocationId
) : ICommand;

internal sealed class MoveWarehouseItemCommandHandler(
    IDbContext db,
    IUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<MoveWarehouseItemCommand>
{
    public async Task<UnitResult<Error>> Handle(
        MoveWarehouseItemCommand request,
        CancellationToken cancellationToken)
    {
        var itemId     = WarehouseItemId.From(request.WarehouseItemId);
        var locationId = WarehouseStorageLocationId.From(request.NewLocationId);
        var now        = clock.UtcNow;

        var item = await db.Set<WarehouseItem>()
            .FirstOrDefaultAsync(w => w.Id == itemId, cancellationToken);
        if (item is null)
            return WarehouseErrors.WarehouseItem.NotFound(request.WarehouseItemId.ToString());

        var location = await db.Set<WarehouseStorageLocation>()
            .FirstOrDefaultAsync(l => l.Id == locationId, cancellationToken);
        if (location is null)
            return WarehouseErrors.StorageLocation.NotFound(request.NewLocationId.ToString());

        if (location.IsOccupied)
            return WarehouseErrors.StorageLocation.Occupied;

        var oldLocationId = item.StorageLocationId;

        var result = item.Move(locationId, location.Label, now);
        if (result.IsFailure)
            return result.Error;

        // ── 3. Update physical occupancy ─────────────────────────────────────
        if (oldLocationId is not null)
        {
            var oldLocation = await db.Set<WarehouseStorageLocation>()
                .FirstOrDefaultAsync(l => l.Id == oldLocationId, cancellationToken);
            oldLocation?.MarkVacant();
        }

        location.MarkOccupied();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}
