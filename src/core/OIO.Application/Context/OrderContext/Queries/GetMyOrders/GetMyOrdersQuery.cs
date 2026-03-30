using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Extensions;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
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
        
        var orders = dbContext.Set<Order>()
            .Where(x => x.BuyerId == currentUser.UserId || x.SellerId == currentUser.UserId);
            
        var totalCounts = await orders.CountAsync(cancellationToken);    
            
        var orderDtos = await orders
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.ToDto())
            .ToPagedListAsync(totalCounts, parameters, cancellationToken);

        return orderDtos;
    }
}
