using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Extensions;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.GetMyInboundShipments;

public sealed record GetMyInboundShipmentsQuery(GetMyInboundShipmentsFilterParameters Parameters) : IQuery<PagedList<InboundShipmentDto>>;

internal sealed class GetMyInboundShipmentsQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyInboundShipmentsQuery, PagedList<InboundShipmentDto>>
{
    public async Task<Result<PagedList<InboundShipmentDto>, Error>> Handle(
        GetMyInboundShipmentsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;

        var query = dbContext.Set<InboundShipment>()
            .Where(s => s.SellerId == currentUser.UserId);

        if (parameters.ItemId.HasValue)
        {
            query = query.Where(s => s.ItemId == parameters.ItemId.Value);
        }

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
