using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.GetOutboundShipmentById;

public sealed record GetOutboundShipmentByIdQuery(Guid ShipmentId)
    : IQuery<OutboundShipmentDto>;

internal sealed class GetOutboundShipmentByIdQueryHandler(IDbContext db)
    : IQueryHandler<GetOutboundShipmentByIdQuery, OutboundShipmentDto>
{
    public async Task<Result<OutboundShipmentDto, Error>> Handle(
        GetOutboundShipmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var shipmentId = OutboundShipmentId.From(request.ShipmentId);

        var shipment = await db.Set<OutboundShipment>()
            .Include(s => s.TrackingEvents)
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return WarehouseErrors.OutboundShipment.NotFound(request.ShipmentId.ToString());

        return shipment.ToDto();
    }
}