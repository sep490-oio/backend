using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Extensions;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.GetMyOutboundShipments;

public sealed record GetMyOutboundShipmentsQuery(PagedParameters Parameters) : IQuery<PagedList<OutboundShipmentDto>>;

internal sealed class GetMyOutboundShipmentsQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyOutboundShipmentsQuery, PagedList<OutboundShipmentDto>>
{
    public async Task<Result<PagedList<OutboundShipmentDto>, Error>> Handle(
        GetMyOutboundShipmentsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;

        // Get outbound shipments where user is buyer or seller of the order
        var query = from s in dbContext.Set<OutboundShipment>()
                    join o in dbContext.Set<Order>() on s.OrderId equals o.Id
                    where o.BuyerId == currentUser.UserId || o.SellerId == currentUser.UserId
                    select s;

        var totalCounts = await query.CountAsync(cancellationToken);

        var pagedShipments = await query
            .AsNoTracking()
            .Include(s => s.TrackingEvents)
            .OrderByDescending(s => s.CreatedAt)
            .Page(parameters)
            .ToListAsync(cancellationToken);

        var dtos = pagedShipments.Select(s => s.ToDto()).ToList();
        return dtos.ToPagedList(totalCounts, parameters);
    }
}
