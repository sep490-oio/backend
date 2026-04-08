using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Application.Extensions;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.GetOutboundShipments;

public record GetOutboundShipmentsQueryFilters : PagedParameters
{
    public string? Status { get; init; }
    public Guid? OrderId { get; init; }
    public string? Search { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}
public sealed record GetOutboundShipmentsQuery(
    GetOutboundShipmentsQueryFilters Parameters
) : IQuery<PagedList<OutboundShipmentDto>>;

internal sealed class GetOutboundShipmentsQueryHandler(IDbContext db)
    : IQueryHandler<GetOutboundShipmentsQuery, PagedList<OutboundShipmentDto>>
{
    public async Task<Result<PagedList<OutboundShipmentDto>, Error>> Handle(
        GetOutboundShipmentsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var query = db.Set<OutboundShipment>()
            .AsNoTracking();
            

        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var status = OutboundShipmentStatus.FromId(parameters.Status.Trim().ToLowerInvariant());
            if (status.HasNoValue)
                return PagedList<OutboundShipmentDto>.Empty();

            query = query.Where(s => s.Status == status.Value);
        }

        if (parameters.OrderId.HasValue)
        {
            var orderId = OrderId.From(parameters.OrderId.Value);
            query = query.Where(s => s.OrderId == orderId);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Search))
            query = query.Where(s =>
                s.CarrierTrackingNumber!.Contains(parameters.Search) ||
                s.ClientOrderCode.Contains(parameters.Search));

        if (parameters.FromDate.HasValue)
            query = query.Where(s => s.CreatedAt >= parameters.FromDate.Value);

        if (parameters.ToDate.HasValue)
            query = query.Where(s => s.CreatedAt <= parameters.ToDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        
        var shipments = await query
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => s.ToDto())
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        return shipments;
    }
}
