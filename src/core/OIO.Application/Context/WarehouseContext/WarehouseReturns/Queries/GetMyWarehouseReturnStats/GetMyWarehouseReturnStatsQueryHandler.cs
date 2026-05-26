using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseToSellerShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.WarehouseReturns.Queries.GetMyWarehouseReturnStats;

internal sealed class GetMyWarehouseReturnStatsQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyWarehouseReturnStatsQuery, SellerWarehouseReturnStatsDto>
{
    public async Task<Result<SellerWarehouseReturnStatsDto, Error>> Handle(
        GetMyWarehouseReturnStatsQuery request,
        CancellationToken ct)
    {
        var sellerId = currentUser.UserId;

        var activeCount = await dbContext.Set<WarehouseToSellerShipment>()
            .AsNoTracking()
            .Where(r => r.SellerId == sellerId && (
                r.Status.Id == WarehouseToSellerShipmentStatus.Pending.Id ||
                r.Status.Id == WarehouseToSellerShipmentStatus.InTransit.Id ||
                r.Status.Id == WarehouseToSellerShipmentStatus.Delivered.Id))
            .CountAsync(ct);

        return new SellerWarehouseReturnStatsDto(activeCount);
    }
}
