using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.WarehouseContext.Queries.GetInboundShipmentById;

public sealed record GetInboundShipmentByIdQuery(Guid ShipmentId)
    : IQuery<InboundShipmentDto>;

internal sealed class GetInboundShipmentByIdQueryHandler(IDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetInboundShipmentByIdQuery, InboundShipmentDto>
{
    public async Task<Result<InboundShipmentDto, Error>> Handle(
        GetInboundShipmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var shipmentId = InboundShipmentId.From(request.ShipmentId);

        var shipment = await db.Set<InboundShipment>()
            .Include(s => s.TrackingEvents)
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return WarehouseErrors.InboundShipment.NotFound(request.ShipmentId.ToString());

        // Ownership check — seller can only view their own shipments;
        // Inspector and WarehouseStaff can view any shipment
        var isStaffRole = currentUser.IsInRole(App.Roles.Catalogs.Inspector)
                       || currentUser.IsInRole(App.Roles.Catalogs.WarehouseStaff)
                       || currentUser.IsInRole(App.Roles.Catalogs.Admin);
        if (!isStaffRole && shipment.SellerId != currentUser.UserId)
            return WarehouseErrors.InboundShipment.NotFound(request.ShipmentId.ToString());

        // Best-effort item enrichment (title + primary image) for the detail view.
        var itemId = ItemId.From(shipment.ItemId);
        var item = await db.Set<Item>()
            .AsNoTracking()
            .Include(i => i.Media)
            .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);

        string? itemTitle = item?.Title.Value;
        var primaryMedia = item?.Media.FirstOrDefault(m => m.IsPrimary)
                           ?? item?.Media.FirstOrDefault();
        string? itemImageUrl = primaryMedia?.Info.SecureUrl;

        return shipment.ToDto() with { ItemTitle = itemTitle, ItemImageUrl = itemImageUrl };
    }
}