using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.GetOutboundShipments;

public sealed record GetOutboundShipmentsQuery(
    string?   Status = null,
    Guid?     OrderId = null,
    string?   Search = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int       Page = 1,
    int       PageSize = 20
) : IQuery<IReadOnlyList<OutboundShipmentDto>>;

internal sealed class GetOutboundShipmentsQueryHandler(IDbContext db)
    : IQueryHandler<GetOutboundShipmentsQuery, IReadOnlyList<OutboundShipmentDto>>
{
    public async Task<Result<IReadOnlyList<OutboundShipmentDto>, Error>> Handle(
        GetOutboundShipmentsQuery request,
        CancellationToken cancellationToken)
    {
        var query = db.Set<OutboundShipment>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(s => s.Status.Id == request.Status);

        if (request.OrderId.HasValue)
            query = query.Where(s => s.OrderId == request.OrderId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(s =>
                s.CarrierTrackingNumber!.Contains(request.Search) ||
                s.ClientOrderCode.Contains(request.Search));

        if (request.FromDate.HasValue)
            query = query.Where(s => s.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(s => s.CreatedAt <= request.ToDate.Value);

        var shipments = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => s.ToDto())
            .ToListAsync(cancellationToken);

        return shipments;
    }
}