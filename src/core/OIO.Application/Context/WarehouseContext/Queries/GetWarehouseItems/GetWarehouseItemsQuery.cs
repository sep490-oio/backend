using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Application.Extensions;
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
        var query = db.Set<WarehouseItem>().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(w => w.Status.Id == request.Status);

        if (request.StorageLocationId.HasValue)
            query = query.Where(w => w.StorageLocationId!.Value == request.StorageLocationId.Value);

        if (request.ItemId.HasValue)
            query = query.Where(w => w.ItemId == request.ItemId.Value);

        if (request.InboundShipmentId.HasValue)
            query = query.Where(w => w.InboundShipmentId.Value == request.InboundShipmentId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => w.ToDto())
            .ToPagedListAsync(totalCount, request, cancellationToken);

        return items;
    }
}