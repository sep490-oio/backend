using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.UpdateStorageLocation;

public sealed record UpdateStorageLocationCommand(
    Guid   LocationId,
    string Zone,
    string Aisle,
    string Shelf,
    string Bin
) : ICommand<StorageLocationDto>;

internal sealed class UpdateStorageLocationCommandHandler(
    IDbContext db,
    IUnitOfWork unitOfWork,
    ILogger<UpdateStorageLocationCommandHandler> logger)
    : ICommandHandler<UpdateStorageLocationCommand, StorageLocationDto>
{
    public async Task<Result<StorageLocationDto, Error>> Handle(
        UpdateStorageLocationCommand request,
        CancellationToken cancellationToken)
    {
        var locationId = WarehouseStorageLocationId.From(request.LocationId);

        var location = await db.Set<WarehouseStorageLocation>()
            .FirstOrDefaultAsync(l => l.Id == locationId, cancellationToken);

        if (location is null)
            return WarehouseErrors.StorageLocation.NotFound(request.LocationId.ToString());

        if (location.IsOccupied)
            return WarehouseErrors.StorageLocation.Occupied;

        var newLabel = $"{request.Zone.Trim().ToUpperInvariant()}-{request.Aisle.Trim()}-{request.Shelf.Trim()}-{request.Bin.Trim()}";
        var labelExists = await db.Set<WarehouseStorageLocation>()
            .AnyAsync(l => l.Label == newLabel && l.Id != locationId, cancellationToken);

        if (labelExists)
            return Error.Conflict(
                "StorageLocation.LabelAlreadyExists",
                $"A storage location with label '{newLabel}' already exists.");

        location.Update(request.Zone, request.Aisle, request.Shelf, request.Bin);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "StorageLocation {LocationId} updated to label {Label}.",
            location.Id.Value, location.Label);

        return location.ToDto();
    }
}