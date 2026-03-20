using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Application.Extensions;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.GetInboundShipments;

public sealed record GetInboundShipmentsQuery(
    string?   Status = null,
    Guid?     SellerId = null,
    Guid?     ItemId = null,
    string?   Search = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int       Page = 1,
    int       PageSize = 20
) : IQuery<PagedList<InboundShipmentDto>>, IPagedParameter
{
    int IPagedParameter.PageNumber => Page < 1 ? 1 : Page;
    int IPagedParameter.PageSize   => PageSize < 1 ? 20 : Math.Min(PageSize, 50);
}

internal sealed class GetInboundShipmentsQueryHandler(IDbContext db)
    : IQueryHandler<GetInboundShipmentsQuery, PagedList<InboundShipmentDto>>
{
    public async Task<Result<PagedList<InboundShipmentDto>, Error>> Handle(
        GetInboundShipmentsQuery request,
        CancellationToken cancellationToken)
    {
        var query = db.Set<InboundShipment>().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(s => s.Status.Id == request.Status);

        if (request.SellerId.HasValue)
            query = query.Where(s => s.SellerId.Value == request.SellerId.Value);

        if (request.ItemId.HasValue)
            query = query.Where(s => s.ItemId == request.ItemId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(s =>
                s.CarrierTrackingNumber!.Contains(request.Search) ||
                s.ClientOrderCode.Contains(request.Search));

        if (request.FromDate.HasValue)
            query = query.Where(s => s.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(s => s.CreatedAt <= request.ToDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var shipments = await query
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => s.ToDto())
            .ToPagedListAsync(totalCount, request, cancellationToken);

        return shipments;
    }
}