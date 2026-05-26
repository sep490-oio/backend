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
        
        List<string>? itemImageUrls = item?.Media
            .OrderBy(m => m.SortOrder)
            .Where(m => !string.IsNullOrEmpty(m.Info.SecureUrl))
            .Select(m => m.Info.SecureUrl!)
            .ToList();

        List<string>? receiptPhotos = null;
        if (shipment.ExtraData is not null)
        {
            try
            {
                var doc = System.Text.Json.JsonDocument.Parse(shipment.ExtraData.RawJson);
                if (doc.RootElement.TryGetProperty("packageReceipt", out var packageReceipt) &&
                    packageReceipt.TryGetProperty("photos", out var photosArr) &&
                    photosArr.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    receiptPhotos = photosArr.EnumerateArray()
                        .Select(x => x.GetString())
                        .Where(x => !string.IsNullOrEmpty(x))
                        .Cast<string>()
                        .ToList();
                }
            }
            catch { /* ignore parsing errors */ }
        }

        InboundShipmentWarehouseItemDto? warehousePackage = null;
        if (isStaffRole)
        {
            var warehouseItem = await db.Set<OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems.WarehouseItem>()
                .AsNoTracking()
                .Include(w => w.Media)
                .FirstOrDefaultAsync(w => w.InboundShipmentId == shipmentId, cancellationToken);

            if (warehouseItem is not null)
            {
                string? storageLocationLabel = null;
                if (warehouseItem.StorageLocationId is not null)
                {
                    var location = await db.Set<OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage.WarehouseStorageLocation>()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(l => l.Id == warehouseItem.StorageLocationId, cancellationToken);
                    storageLocationLabel = location?.Label;
                }

                warehousePackage = new InboundShipmentWarehouseItemDto(
                    Id: warehouseItem.Id.Value,
                    Status: warehouseItem.Status.Id,
                    StorageLocationLabel: storageLocationLabel,
                    Media: warehouseItem.Media
                        .OrderBy(m => m.SortOrder)
                        .Select(m => new WarehouseItemMediaDto(
                            Id: m.Id.Value,
                            ResourceType: m.ResourceType,
                            IsPrimary: m.IsPrimary,
                            SortOrder: m.SortOrder,
                            SecureUrl: m.Info.SecureUrl!,
                            FileName: m.Info.FileName))
                        .ToList()
                );
            }
        }

        return shipment.ToDto() with { 
            ItemTitle = itemTitle, 
            ItemImageUrl = itemImageUrl,
            ItemImageUrls = itemImageUrls,
            ReceiptPhotos = receiptPhotos,
            WarehousePackage = warehousePackage
        };
    }
}