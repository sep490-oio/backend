using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.AdjustWarehouseItem;

public sealed record AdjustWarehouseItemCommand(
    Guid   WarehouseItemId,
    string NewStatus,
    string Reason
) : ICommand;

internal sealed class AdjustWarehouseItemCommandHandler(
    IDbContext db,
    IUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<AdjustWarehouseItemCommand>
{
    public async Task<UnitResult<Error>> Handle(
        AdjustWarehouseItemCommand request,
        CancellationToken cancellationToken)
    {
        var itemId     = WarehouseItemId.From(request.WarehouseItemId);
        var statusId   = request.NewStatus.ToLower().Trim();
        var now        = clock.UtcNow;

        var item = await db.Set<WarehouseItem>()
            .FirstOrDefaultAsync(w => w.Id == itemId, cancellationToken);
        if (item is null)
            return WarehouseErrors.WarehouseItem.NotFound(request.WarehouseItemId.ToString());

        var maybeStatus = WarehouseItemStatus.FromId(statusId);
        if (maybeStatus.HasNoValue)
            return Error.Validation(nameof(request.NewStatus), "InvalidStatus", $"Status '{request.NewStatus}' is not valid.");

        var oldLocationId = item.StorageLocationId;

        var result = item.AdjustStatus(maybeStatus.Value, now);
        if (result.IsFailure)
            return result.Error;

        // ── 3. Manage physical occupancy ─────────────────────────────────────
        // If the domain method cleared the StorageLocationId, we must mark the physical shelf as vacant.
        if (oldLocationId is not null && item.StorageLocationId is null)
        {
            var location = await db.Set<WarehouseStorageLocation>()
                .FirstOrDefaultAsync(l => l.Id == oldLocationId, cancellationToken);
            location?.MarkVacant();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}
