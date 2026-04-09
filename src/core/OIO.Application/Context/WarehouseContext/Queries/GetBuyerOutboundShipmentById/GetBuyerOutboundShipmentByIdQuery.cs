using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetBuyerOutboundShipmentByToken;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.GetBuyerOutboundShipmentById;

public sealed record GetBuyerOutboundShipmentByIdQuery(Guid ShipmentId)
    : IQuery<BuyerOutboundShipmentDetailDto>;

internal sealed class GetBuyerOutboundShipmentByIdQueryHandler(
    IDbContext db,
    ICurrentUser currentUser)
    : IQueryHandler<GetBuyerOutboundShipmentByIdQuery, BuyerOutboundShipmentDetailDto>
{
    public async Task<Result<BuyerOutboundShipmentDetailDto, Error>> Handle(
        GetBuyerOutboundShipmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var shipmentId = OutboundShipmentId.From(request.ShipmentId);
        var shipment = await db.Set<OutboundShipment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return Error.NotFound("OutboundShipment.NotFound", "Shipment was not found.");

        var order = await db.Set<Order>()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == shipment.OrderId, cancellationToken);

        if (order is null || order.BuyerId != currentUser.UserId)
            return Error.NotFound("OutboundShipment.NotFound", "Shipment was not found.");

        var detail = await BuyerOutboundShipmentDetailBuilder.BuildAsync(db, shipment, order, cancellationToken);
        // Strip action flags — this endpoint is read-only. Action flags are
        // only exposed through the token-based QR query (GetBuyerOutboundShipmentByTokenQuery).
        return detail with
        {
            CanAccept = false,
            CanSubmitReceiptProof = false,
            CanOpenDispute = false,
            CanSubmitProof = false,
            CanAcknowledgeReceived = false,
        };
    }
}
