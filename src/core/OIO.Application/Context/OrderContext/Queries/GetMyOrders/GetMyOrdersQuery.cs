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
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using SellerDirectShipmentEntity = OIO.Domain.Context.OrderContext.Aggregates.SellerDirectShipments.SellerDirectShipment;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Queries.GetMyOrders;

public record GetMyOrdersQueryFilter : PagedParameters;
public sealed record GetMyOrdersQuery(GetMyOrdersQueryFilter Parameters) : IQuery<PagedList<OrderDto>>;

internal sealed class GetMyOrdersQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyOrdersQuery, PagedList<OrderDto>>
{
    public async Task<Result<PagedList<OrderDto>, Error>> Handle(
        GetMyOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var pageNumber = parameters.PageNumber ?? 1;
        var pageSize = parameters.PageSize ?? 20;

        var ordersQuery = dbContext.Set<Order>()
            .AsNoTracking()
            .Include(o => o.Return)
            .Include(o => o.Escrows)
            .Include(o => o.OutboundShipments)
            .Where(x => x.BuyerId == currentUser.UserId || x.SellerId == currentUser.UserId);

        var totalCounts = await ordersQuery.CountAsync(cancellationToken);

        // Materialize the page of orders first — we need the entities so we can
        // batch-fetch their auctions in a single follow-up query (N+1-safe).
        var orders = await ordersQuery
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Batch-load related auctions + items so every OrderDto carries its
        // own product summary without per-row queries.
        var auctionIds = orders.Select(o => o.AuctionId).Distinct().ToList();
        var auctionsById = new Dictionary<AuctionId, Auction>();
        if (auctionIds.Count > 0)
        {
            var auctions = await dbContext.Set<Auction>()
                .AsNoTracking()
                .Include(a => a.Item)
                    .ThenInclude(i => i.Media)
                .Where(a => auctionIds.Contains(a.Id))
                .ToListAsync(cancellationToken);

            auctionsById = auctions.ToDictionary(a => a.Id);
        }

        // Batch-load direct shipments keyed by OrderId so each OrderDto
        // carries its 1:1 SellerDirectShipmentEntity without per-row queries.
        var orderIds = orders.Select(o => o.Id).ToList();
        var directShipmentsByOrderId = new Dictionary<OrderId, SellerDirectShipmentEntity>();
        if (orderIds.Count > 0)
        {
            var shipments = await dbContext.Set<SellerDirectShipmentEntity>()
                .AsNoTracking()
                .Include(s => s.Evidence)
                .Where(s => orderIds.Contains(s.OrderId))
                .ToListAsync(cancellationToken);
            directShipmentsByOrderId = shipments.ToDictionary(s => s.OrderId);
        }

        var items = orders
            .Select(o =>
            {
                var auction = auctionsById.GetValueOrDefault(o.AuctionId);
                var directShipment = directShipmentsByOrderId.GetValueOrDefault(o.Id);
                return o.ToDto(item: o.ToItemSummary(auction), directShipment: directShipment);
            })
            .ToList();

        return new PagedList<OrderDto>(items, totalCounts, pageNumber, pageSize);
    }
}
