using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
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
        var query = db.Set<WarehouseItem>()
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = WarehouseItemStatus.FromId(request.Status.Trim().ToLowerInvariant());
            if (status.HasNoValue)
                return Array.Empty<WarehouseItemDto>();

            query = query.Where(w => w.Status == status.Value);
        }

        if (request.StorageLocationId.HasValue)
        {
            var storageLocationId = WarehouseStorageLocationId.From(request.StorageLocationId.Value);
            query = query.Where(w => w.StorageLocationId == storageLocationId);
        }

        if (request.ItemId.HasValue)
            query = query.Where(w => w.ItemId == request.ItemId.Value);

        if (request.InboundShipmentId.HasValue)
        {
            var inboundShipmentId = InboundShipmentId.From(request.InboundShipmentId.Value);
            query = query.Where(w => w.InboundShipmentId == inboundShipmentId);
        }

        var items = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return items.Select(w => w.ToDto()).ToList();
    }
}
