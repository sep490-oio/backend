using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Orders.Queries.GetMyOrderStats;

internal sealed class GetMyOrderStatsQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyOrderStatsQuery, SellerOrderStatsDto>
{
    public async Task<Result<SellerOrderStatsDto, Error>> Handle(
        GetMyOrderStatsQuery request,
        CancellationToken ct)
    {
        var sellerId = currentUser.UserId;

        var baseQuery = dbContext.Set<Order>()
            .AsNoTracking()
            .Where(o => o.SellerId == sellerId);

        var awaitingShipmentCount = await baseQuery
            .Where(o => o.Status.Id == OrderStatus.Processing.Id || o.Status.Id == OrderStatus.Paid.Id)
            .CountAsync(ct);

        // Calculate OrderReturns (e.g. status = ReturnRequested, Returning, Returned, etc.)
        // But for simplicity, we can just say ReturnRequested for now, or check o.Returns
        var returnCount = await dbContext.Set<OrderReturn>()
            .AsNoTracking()
            .Where(r => r.Order.SellerId == sellerId && r.Status.Id == OrderReturnStatus.Requested.Id)
            .CountAsync(ct);

        return new SellerOrderStatsDto(awaitingShipmentCount, returnCount);
    }
}
