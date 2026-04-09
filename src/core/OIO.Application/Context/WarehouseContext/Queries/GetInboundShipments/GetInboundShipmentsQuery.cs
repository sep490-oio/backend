using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.GetInboundShipments;

public record GetInboundShipmentsQueryFilter : PagedParameters
{
    public string?   Status { get; init; }
    public Guid?     SellerId { get; init; }
    public Guid?     ItemId { get; init; }
    public string?   Search { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public bool?     RequiresPlatformInspection { get; init; }
}

public sealed record GetInboundShipmentsQuery(
    GetInboundShipmentsQueryFilter Parameters
) : IQuery<PagedList<InboundShipmentDto>>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetInboundShipmentsQuery.Check()
            .WithOwnerName("GetInboundShipments")
            .Field(Parameters.Status)
            .WhenHasValue(x => x.InSet(InboundShipmentStatus.All.Select(status => status.Id)));
    }
}

internal sealed class GetInboundShipmentsQueryHandler(IDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetInboundShipmentsQuery, PagedList<InboundShipmentDto>>
{
    public async Task<Result<PagedList<InboundShipmentDto>, Error>> Handle(
        GetInboundShipmentsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var isStaffRole = currentUser.IsInRole(App.Roles.Catalogs.WarehouseStaff)
                       || currentUser.IsInRole(App.Roles.Catalogs.Inspector)
                       || currentUser.IsInRole(App.Roles.Catalogs.Admin);
        var query = db.Set<InboundShipment>().AsNoTracking();

        // Sellers see only their own shipments; staff sees all
        if (!isStaffRole)
            query = query.Where(s => s.SellerId == currentUser.UserId);

        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var status = InboundShipmentStatus.FromId(parameters.Status.Trim().ToLowerInvariant());
            query = query.Where(s => s.Status == status.Value);
        }

        if (parameters.SellerId.HasValue)
        {
            var sellerId = UserId.From(parameters.SellerId.Value);
            query = query.Where(s => s.SellerId == sellerId);
        }

        if (parameters.ItemId.HasValue)
            query = query.Where(s => s.ItemId == parameters.ItemId.Value);

        if (!string.IsNullOrWhiteSpace(parameters.Search))
            query = query.Where(s =>
                s.CarrierTrackingNumber!.Contains(parameters.Search) ||
                s.ClientOrderCode.Contains(parameters.Search));

        if (parameters.FromDate.HasValue)
            query = query.Where(s => s.CreatedAt >= parameters.FromDate.Value);

        if (parameters.ToDate.HasValue)
            query = query.Where(s => s.CreatedAt <= parameters.ToDate.Value);

        if (parameters.RequiresPlatformInspection.HasValue)
        {
            // Source of truth is Item.RequiresPlatformInspection (set at Submit/Resubmit).
            // Auction.VerifyByPlatform is only a compatibility snapshot and no longer queried.
            var flag = parameters.RequiresPlatformInspection.Value;
            var itemIdsWithFlag = db.Set<OIO.Domain.Context.CatalogContext.Aggregates.Items.Item>()
                .Where(i => i.RequiresPlatformInspection == flag)
                .Select(i => i.Id.Value);
            query = query.Where(s => itemIdsWithFlag.Contains(s.ItemId));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        
        var shipments = await query
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => s.ToDto())
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        return shipments;
    }
}
