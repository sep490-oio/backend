using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.GetWarehouseItems;

public sealed record GetWarehouseItemsQuery(
    string? Status = null,
    Guid?   StorageLocationId = null,
    Guid?   ItemId = null,
    Guid?   InboundShipmentId = null,
    int     Page = 1,
    int     PageSize = 20
) : IQuery<IReadOnlyList<WarehouseItemDto>>;

internal sealed class GetWarehouseItemsQueryHandler(IDbContext db)
    : IQueryHandler<GetWarehouseItemsQuery, IReadOnlyList<WarehouseItemDto>>
{
    public async Task<Result<IReadOnlyList<WarehouseItemDto>, Error>> Handle(
        GetWarehouseItemsQuery request,
        CancellationToken cancellationToken)
    {
        var query = db.Set<WarehouseItem>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(w => w.Status.Id == request.Status);

        if (request.StorageLocationId.HasValue)
            query = query.Where(w => w.StorageLocationId!.Value == request.StorageLocationId.Value);

        if (request.ItemId.HasValue)
            query = query.Where(w => w.ItemId == request.ItemId.Value);

        if (request.InboundShipmentId.HasValue)
            query = query.Where(w => w.InboundShipmentId.Value == request.InboundShipmentId.Value);

        var items = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(w => w.ToDto())
            .ToListAsync(cancellationToken);

        return items;
    }
}