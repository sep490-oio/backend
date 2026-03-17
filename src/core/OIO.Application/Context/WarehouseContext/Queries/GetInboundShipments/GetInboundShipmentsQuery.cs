using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.GetInboundShipments;

public sealed record GetInboundShipmentsQuery(
    string?   Status = null,
    Guid?     SellerId = null,
    Guid?     ItemId = null,
    string?   Search = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int       Page = 1,
    int       PageSize = 20
) : IQuery<IReadOnlyList<InboundShipmentDto>>;

internal sealed class GetInboundShipmentsQueryHandler(IDbContext db)
    : IQueryHandler<GetInboundShipmentsQuery, IReadOnlyList<InboundShipmentDto>>
{
    public async Task<Result<IReadOnlyList<InboundShipmentDto>, Error>> Handle(
        GetInboundShipmentsQuery request,
        CancellationToken cancellationToken)
    {
        var query = db.Set<InboundShipment>()
            .AsNoTracking()
            .Include(s => s.TrackingEvents)
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = InboundShipmentStatus.FromId(request.Status.Trim().ToLowerInvariant());
            if (status.HasNoValue)
                return Array.Empty<InboundShipmentDto>();

            query = query.Where(s => s.Status == status.Value);
        }

        if (request.SellerId.HasValue)
        {
            var sellerId = UserId.From(request.SellerId.Value);
            query = query.Where(s => s.SellerId == sellerId);
        }

        if (request.ItemId.HasValue)
            query = query.Where(s => s.ItemId == request.ItemId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(s =>
                s.CarrierTrackingNumber!.Contains(request.Search) ||
                s.ClientOrderCode.Contains(request.Search));

        if (request.FromDate.HasValue)
            query = query.Where(s => s.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(s => s.CreatedAt <= request.ToDate.Value);

        var shipments = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return shipments.Select(s => s.ToDto()).ToList();
    }
}
