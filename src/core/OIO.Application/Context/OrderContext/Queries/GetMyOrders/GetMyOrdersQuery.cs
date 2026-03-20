using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Queries.GetMyOrders;

public sealed record GetMyOrdersQuery() : IQuery<IReadOnlyList<OrderDto>>;

internal sealed class GetMyOrdersQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyOrdersQuery, IReadOnlyList<OrderDto>>
{
    public async Task<Result<IReadOnlyList<OrderDto>, Error>> Handle(
        GetMyOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var orders = await dbContext.Set<Order>()
            .AsNoTracking()
            .Include(x => x.Return)
            .Include(x => x.Escrows)
            .Include(x => x.OutboundShipments)
            .Where(x => x.BuyerId == currentUser.UserId || x.SellerId == currentUser.UserId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return orders.Select(x => x.ToDto()).ToList();
    }
}
