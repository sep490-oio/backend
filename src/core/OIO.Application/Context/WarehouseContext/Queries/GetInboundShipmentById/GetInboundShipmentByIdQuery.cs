using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.GetInboundShipmentById;

public sealed record GetInboundShipmentByIdQuery(Guid ShipmentId)
    : IQuery<InboundShipmentDto>;

internal sealed class GetInboundShipmentByIdQueryHandler(IDbContext db)
    : IQueryHandler<GetInboundShipmentByIdQuery, InboundShipmentDto>
{
    public async Task<Result<InboundShipmentDto, Error>> Handle(
        GetInboundShipmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var shipmentId = InboundShipmentId.From(request.ShipmentId);

        var shipment = await db.Set<InboundShipment>()
            .Include(s => s.TrackingEvents)
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return WarehouseErrors.InboundShipment.NotFound(request.ShipmentId.ToString());

        return shipment.ToDto();
    }
}