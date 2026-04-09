using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetInboundPackages;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.WarehouseContext.Queries.GetInboundPackageByCode;

public sealed record GetInboundPackageByCodeQuery(string ClientOrderCode) : IQuery<InboundPackageDetailDto>;

internal sealed class GetInboundPackageByCodeQueryHandler(IDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetInboundPackageByCodeQuery, InboundPackageDetailDto>
{
    public async Task<Result<InboundPackageDetailDto, Error>> Handle(
        GetInboundPackageByCodeQuery request,
        CancellationToken cancellationToken)
    {
        var code = request.ClientOrderCode;

        var siblings = await db.Set<InboundShipment>()
            .AsNoTracking()
            .Where(s => s.ClientOrderCode == code)
            .ToListAsync(cancellationToken);

        if (siblings.Count == 0)
            return WarehouseErrors.InboundShipment.NotFound(code);

        var isStaffRole = currentUser.IsInRole(App.Roles.Catalogs.WarehouseStaff)
                       || currentUser.IsInRole(App.Roles.Catalogs.Inspector)
                       || currentUser.IsInRole(App.Roles.Catalogs.Admin);

        if (!isStaffRole && siblings.Any(s => s.SellerId != currentUser.UserId))
            return WarehouseErrors.InboundShipment.NotFound(code);

        var shipmentIds = siblings.Select(s => s.Id).ToList();
        var warehouseItems = await db.Set<WarehouseItem>()
            .AsNoTracking()
            .Where(w => shipmentIds.Contains(w.InboundShipmentId))
            .ToListAsync(cancellationToken);

        var wiByShipment = warehouseItems.ToDictionary(w => w.InboundShipmentId);

        var itemIds = siblings.Select(s => ItemId.From(s.ItemId)).Distinct().ToList();
        var items = await db.Set<Item>()
            .AsNoTracking()
            .Include(i => i.Media)
            .Where(i => itemIds.Contains(i.Id))
            .ToListAsync(cancellationToken);
        var itemsById = items.ToDictionary(i => i.Id.Value);

        var locationIds = warehouseItems
            .Where(w => w.StorageLocationId != null)
            .Select(w => w.StorageLocationId!)
            .Distinct()
            .ToList();
        var locations = await db.Set<WarehouseStorageLocation>()
            .AsNoTracking()
            .Where(l => locationIds.Contains(l.Id))
            .ToListAsync(cancellationToken);
        var locationsById = locations.ToDictionary(l => l.Id);

        var first = siblings.First();
        var state = PackageStateResolver.Resolve(siblings, wiByShipment);
        var display = PackageStateResolver.ResolveDisplay(siblings, wiByShipment);
        var (media, notes) = PackageStateResolver.ExtractReceipt(first);
        var firstReceivedAt = siblings.Where(x => x.ArrivedAt.HasValue).Min(x => x.ArrivedAt);
        var createdAt = siblings.Min(x => x.CreatedAt);

        var itemDtos = siblings.Select(s =>
        {
            itemsById.TryGetValue(s.ItemId, out var item);
            wiByShipment.TryGetValue(s.Id, out var wi);
            string? locationLabel = null;
            if (wi?.StorageLocationId is { } locId && locationsById.TryGetValue(locId, out var loc))
                locationLabel = loc.Label;

            var primaryImage = item?.Media.FirstOrDefault(m => m.IsPrimary)?.Info.SecureUrl
                               ?? item?.Media.FirstOrDefault()?.Info.SecureUrl;

            return new InboundPackageItemDto(
                InboundShipmentId: s.Id.Value,
                ItemId: s.ItemId,
                ItemTitle: item?.Title.Value,
                ItemImageUrl: primaryImage,
                InboundStatus: s.Status.Id,
                WarehouseItemId: wi?.Id.Value,
                WarehouseItemStatus: wi?.Status.Id,
                StorageLocationId: wi?.StorageLocationId?.Value,
                StorageLocationLabel: locationLabel);
        }).ToList();

        return new InboundPackageDetailDto(
            ClientOrderCode: code,
            ProviderCode: first.ProviderCode.Id,
            ShipmentMode: first.ShipmentMode.Id,
            ExternalCarrierName: first.ExternalCarrierName,
            CarrierTrackingNumber: first.CarrierTrackingNumber,
            SenderName: first.SenderName,
            SenderPhone: first.SenderPhone,
            SenderAddress: first.SenderAddress,
            SenderWard: first.SenderWard,
            SenderDistrict: first.SenderDistrict,
            SenderProvince: first.SenderProvince,
            ExpectedArrivalAt: first.ExpectedArrivalAt,
            PackageState: state,
            FirstReceivedAt: firstReceivedAt,
            ReceiptMedia: media,
            ReceiptNotes: notes,
            Items: itemDtos,
            CreatedAt: createdAt,
            ShippingFee: first.ShippingFee,
            DisplayStatus: display,
            PackageQrToken: $"pkg:{code}",
            CanCancelPackage: PackageStateResolver.CanCancel(siblings, wiByShipment));
    }
}
