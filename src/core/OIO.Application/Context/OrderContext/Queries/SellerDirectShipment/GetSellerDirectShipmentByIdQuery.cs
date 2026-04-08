using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using ShipmentAggregate = OIO.Domain.Context.OrderContext.Aggregates.SellerDirectShipments.SellerDirectShipment;

namespace OIO.Application.Context.OrderContext.Queries.SellerDirectShipment;

/// <summary>Seller-scoped detail query. Mismatches → 404.</summary>
public sealed record GetSellerDirectShipmentByIdQuery(Guid ShipmentId)
    : IQuery<SellerDirectShipmentDto>;

internal sealed class GetSellerDirectShipmentByIdQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetSellerDirectShipmentByIdQuery, SellerDirectShipmentDto>
{
    public async Task<Result<SellerDirectShipmentDto, Error>> Handle(
        GetSellerDirectShipmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var shipmentId = SellerDirectShipmentId.From(request.ShipmentId);
        var shipment = await dbContext.Set<ShipmentAggregate>()
            .AsNoTracking()
            .Include(s => s.Evidence)
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return Error.NotFound("SellerDirectShipment.NotFound", "Direct shipment was not found.");

        var order = await dbContext.Set<Order>()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == shipment.OrderId, cancellationToken);

        if (order is null || order.SellerId != currentUser.UserId)
            return Error.NotFound("SellerDirectShipment.NotFound", "Direct shipment was not found.");

        return shipment.ToDto();
    }
}
