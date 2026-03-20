using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Application.Extensions;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.GetStorageLocations;

public sealed record GetStorageLocationsQuery(
    bool    VacantOnly = false,
    string? Zone = null,
    string? Search = null,
    int     Page = 1,
    int     PageSize = 50
) : IQuery<PagedList<StorageLocationDto>>, IPagedParameter
{
    int IPagedParameter.PageNumber => Page < 1 ? 1 : Page;
    int IPagedParameter.PageSize   => PageSize < 1 ? 50 : Math.Min(PageSize, 100);
}

internal sealed class GetStorageLocationsQueryHandler(IDbContext db)
    : IQueryHandler<GetStorageLocationsQuery, PagedList<StorageLocationDto>>
{
    public async Task<Result<PagedList<StorageLocationDto>, Error>> Handle(
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

        var totalCount = await query.CountAsync(cancellationToken);

        var locations = await query
            .OrderBy(l => l.Zone)
            .ThenBy(l => l.Aisle)
            .ThenBy(l => l.Shelf)
            .ThenBy(l => l.Bin)
            .Select(l => l.ToDto())
            .ToPagedListAsync(totalCount, request, cancellationToken);

        return locations;
    }
}
