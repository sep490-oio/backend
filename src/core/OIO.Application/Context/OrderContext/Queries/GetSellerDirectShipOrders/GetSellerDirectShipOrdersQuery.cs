using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Queries.GetSellerDirectShipOrders;

public sealed record GetSellerDirectShipOrdersQuery(PagedParameters Parameters) : IQuery<PagedList<OrderDto>>;

internal sealed class GetSellerDirectShipOrdersQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetSellerDirectShipOrdersQuery, PagedList<OrderDto>>
{
    public async Task<Result<PagedList<OrderDto>, Error>> Handle(
        GetSellerDirectShipOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;

        // Seller's paid orders
        var ordersQuery = dbContext.Set<Order>()
            .Where(o => o.SellerId == currentUser.UserId && 
                       (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Processing));

        // Filter: Item is NOT in warehouse (no WarehouseItem exists or all are Dispatched)
        var directShipOrdersQuery = from o in ordersQuery
                                    join a in dbContext.Set<Auction>() on o.AuctionId equals a.Id
                                    where !dbContext.Set<WarehouseItem>().Any(wi => 
                                        wi.ItemId == a.Item.Id.Value && 
                                        wi.Status != WarehouseItemStatus.Dispatched)
                                    select o;

        var totalCounts = await directShipOrdersQuery.CountAsync(cancellationToken);

        var pagedOrders = await directShipOrdersQuery
            .Include(x => x.Return)
            .Include(x => x.Escrows)
            .Include(x => x.OutboundShipments)
            .OrderByDescending(x => x.CreatedAt)
            .Page(parameters)
            .ToListAsync(cancellationToken);

        var dtos = pagedOrders.Select(x => x.ToDto()).ToList();
        return dtos.ToPagedList(totalCounts, parameters);
    }
}
