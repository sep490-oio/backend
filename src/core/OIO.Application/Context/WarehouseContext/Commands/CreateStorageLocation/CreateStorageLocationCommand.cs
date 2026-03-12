using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.CreateStorageLocation;

public sealed record CreateStorageLocationCommand(
    string Zone,
    string Aisle,
    string Shelf,
    string Bin
) : ICommand<StorageLocationDto>;

internal sealed class CreateStorageLocationCommandHandler(
    IDbContext db,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<CreateStorageLocationCommandHandler> logger)
    : ICommandHandler<CreateStorageLocationCommand, StorageLocationDto>
{
    public async Task<Result<StorageLocationDto, Error>> Handle(
        CreateStorageLocationCommand request,
        CancellationToken cancellationToken)
    {
        var now   = clock.UtcNow;
        var zone  = request.Zone?.Trim().ToUpperInvariant();
        var aisle = request.Aisle?.Trim();
        var shelf = request.Shelf?.Trim();
        var bin   = request.Bin?.Trim();

        var label = $"{zone}-{aisle}-{shelf}-{bin}";
        // Duplicate label guard
        var exists = await db.Set<WarehouseStorageLocation>()
            .AnyAsync(l => l.Label == label, cancellationToken);

        if (exists)
            return Error.Conflict(
                "StorageLocation.LabelAlreadyExists",
                $"A storage location with label '{label}' already exists.");

        var location = WarehouseStorageLocation.Create(
            zone:  request.Zone,
            aisle: request.Aisle,
            shelf: request.Shelf,
            bin:   request.Bin,
            now:   now);

        db.Set<WarehouseStorageLocation>().Add(location);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "StorageLocation {Label} created with id {LocationId}.",
            location.Label, location.Id.Value);

        return location.ToDto();
    }
}