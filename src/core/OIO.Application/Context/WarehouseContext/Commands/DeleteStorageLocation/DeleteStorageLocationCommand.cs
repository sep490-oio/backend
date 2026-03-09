using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.DeleteStorageLocation;

public sealed record DeleteStorageLocationCommand(Guid LocationId) : ICommand;

internal sealed class DeleteStorageLocationCommandHandler(
    IDbContext db,
    IUnitOfWork unitOfWork,
    ILogger<DeleteStorageLocationCommandHandler> logger)
    : ICommandHandler<DeleteStorageLocationCommand>
{
    public async Task<UnitResult<Error>> Handle(
        DeleteStorageLocationCommand request,
        CancellationToken cancellationToken)
    {
        var locationId = WarehouseStorageLocationId.From(request.LocationId);

        var location = await db.Set<WarehouseStorageLocation>()
            .FirstOrDefaultAsync(l => l.Id == locationId, cancellationToken);

        if (location is null)
            return WarehouseErrors.StorageLocation.NotFound(request.LocationId.ToString());

        if (location.IsOccupied)
            return WarehouseErrors.StorageLocation.Occupied;

        db.Set<WarehouseStorageLocation>().Remove(location);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "StorageLocation {Label} ({LocationId}) deleted.",
            location.Label, location.Id.Value);

        return UnitResult.Success<Error>();
    }
}