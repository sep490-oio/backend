using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.GetStorageLocations;

public sealed record GetStorageLocationsQuery(
    bool    VacantOnly = false,
    string? Zone = null,
    string? Search = null,
    int     Page = 1,
    int     PageSize = 50
) : IQuery<IReadOnlyList<StorageLocationDto>>;

internal sealed class GetStorageLocationsQueryHandler(IDbContext db)
    : IQueryHandler<GetStorageLocationsQuery, IReadOnlyList<StorageLocationDto>>
{
    public async Task<Result<IReadOnlyList<StorageLocationDto>, Error>> Handle(
        GetStorageLocationsQuery request,
        CancellationToken cancellationToken)
    {
        var query = db.Set<WarehouseStorageLocation>()
            .AsNoTracking()
            .AsQueryable();

        if (request.VacantOnly)
            query = query.Where(l => !l.IsOccupied);

        if (!string.IsNullOrWhiteSpace(request.Zone))
            query = query.Where(l => l.Zone == request.Zone.Trim().ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(l => l.Label.Contains(request.Search));

        var locations = await query
            .OrderBy(l => l.Zone)
            .ThenBy(l => l.Aisle)
            .ThenBy(l => l.Shelf)
            .ThenBy(l => l.Bin)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return locations.Select(l => l.ToDto()).ToList();
    }
}
