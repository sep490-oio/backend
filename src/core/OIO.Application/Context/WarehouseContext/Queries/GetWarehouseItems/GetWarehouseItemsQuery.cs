using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Application.Extensions;
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
) : IQuery<PagedList<WarehouseItemDto>>, IPagedParameter
{
    int IPagedParameter.PageNumber => Page < 1 ? 1 : Page;
    int IPagedParameter.PageSize   => PageSize < 1 ? 20 : Math.Min(PageSize, 50);
}

internal sealed class GetWarehouseItemsQueryHandler(IDbContext db)
    : IQueryHandler<GetWarehouseItemsQuery, PagedList<WarehouseItemDto>>
{
    public async Task<Result<PagedList<WarehouseItemDto>, Error>> Handle(
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
                return new PagedList<WarehouseItemDto>(Array.Empty<WarehouseItemDto>(), 0, 1, request.PageSize);

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

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => w.ToDto())
            .ToPagedListAsync(totalCount, request, cancellationToken);

        return items;
    }
}
