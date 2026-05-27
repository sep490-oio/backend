using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Application.Abstractions.Sorting;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Application.Extensions;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.WarehouseContext.Queries.GetInboundPackages;

public record GetInboundPackagesQueryFilter : PagedParameters, ISortByParameter
{
    public string? PackageState { get; init; }
    public string? Search { get; init; }
    public string? SortBy { get; init; }
}

public static class PackageStateFilters
{
    public const string AwaitingPickup = "awaiting_pickup";
    public const string InTransit = "in_transit";
    public const string PendingArrival = "pending_arrival";
    public const string Arrived = "arrived";
    public const string Inspected = "inspected";
    public const string Stored = "stored";
    public const string Received = "received";
    public const string Cancelled = "cancelled";
}

public sealed record GetInboundPackagesQuery(
    GetInboundPackagesQueryFilter Parameters) : IQuery<PagedList<InboundPackageDto>>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetInboundPackagesQuery.Check()
            .WithOwnerName("GetInboundPackages")
            .Field(Parameters.SortBy)
            .WhenHasValue(x => x.Must(InboundShipmentMappings.InboundPackageDtoSortMapping.ValidateMappings));
    }
}

internal sealed class GetInboundPackagesQueryHandler(IDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetInboundPackagesQuery, PagedList<InboundPackageDto>>
{
    public async Task<Result<PagedList<InboundPackageDto>, Error>> Handle(
        GetInboundPackagesQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;

        var isStaffRole = currentUser.IsInRole(App.Roles.Catalogs.WarehouseStaff)
                       || currentUser.IsInRole(App.Roles.Catalogs.Inspector)
                       || currentUser.IsInRole(App.Roles.Catalogs.Admin);

        var query = db.Set<InboundShipment>().AsNoTracking();

        if (!isStaffRole)
            query = query.Where(s => s.SellerId == currentUser.UserId);

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var s = parameters.Search.Trim().ToLower();
            query = query.Where(x =>
                x.ClientOrderCode.ToLower().Contains(s) ||
                (x.CarrierTrackingNumber != null && x.CarrierTrackingNumber.ToLower().Contains(s)) ||
                x.SenderName.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(parameters.PackageState))
        {
            var state = parameters.PackageState.Trim().ToLower();
            IQueryable<string> matchingKeys;
            var allCodes = db.Set<InboundShipment>().Select(s => s.ClientOrderCode).Distinct();

            if (state == PackageStateFilters.AwaitingPickup)
            {
                matchingKeys = db.Set<InboundShipment>().Where(s => s.Status == InboundShipmentStatus.AwaitingPickup).Select(s => s.ClientOrderCode);
            }
            else if (state == PackageStateFilters.InTransit)
            {
                matchingKeys = db.Set<InboundShipment>().Where(s => s.Status == InboundShipmentStatus.InTransit || s.Status == InboundShipmentStatus.Delivering).Select(s => s.ClientOrderCode);
            }
            else if (state == PackageStateFilters.PendingArrival)
            {
                // NO shipment in the package has a WarehouseItem
                matchingKeys = allCodes.Where(code => 
                    !db.Set<WarehouseItem>().Any(w => 
                        db.Set<InboundShipment>().Any(s => s.Id == w.InboundShipmentId && s.ClientOrderCode == code)
                    )
                );
            }
            else if (state == PackageStateFilters.Arrived)
            {
                matchingKeys = db.Set<InboundShipment>().Where(s => s.Status == InboundShipmentStatus.Arrived || s.Status == InboundShipmentStatus.Inspected || s.Status == InboundShipmentStatus.Completed).Select(s => s.ClientOrderCode);
            }
            else if (state == PackageStateFilters.Inspected)
            {
                // ALL shipments have a WarehouseItem AND ALL WarehouseItems are (Inspected | Reserved | Dispatched)
                matchingKeys = allCodes.Where(code => 
                    db.Set<InboundShipment>().Count(s => s.ClientOrderCode == code) == db.Set<WarehouseItem>().Count(w => db.Set<InboundShipment>().Any(s => s.Id == w.InboundShipmentId && s.ClientOrderCode == code))
                    &&
                    !db.Set<WarehouseItem>().Any(w => 
                        db.Set<InboundShipment>().Any(s => s.Id == w.InboundShipmentId && s.ClientOrderCode == code) &&
                        w.Status != WarehouseItemStatus.Inspected &&
                        w.Status != WarehouseItemStatus.Reserved &&
                        w.Status != WarehouseItemStatus.Dispatched
                    )
                );
            }
            else if (state == PackageStateFilters.Stored)
            {
                // ALL shipments have a WarehouseItem AND NO WarehouseItem is Pending/Received/Lost/Damaged 
                // AND AT LEAST ONE WarehouseItem is Stored
                matchingKeys = allCodes.Where(code => 
                    db.Set<InboundShipment>().Count(s => s.ClientOrderCode == code) == db.Set<WarehouseItem>().Count(w => db.Set<InboundShipment>().Any(s => s.Id == w.InboundShipmentId && s.ClientOrderCode == code))
                    &&
                    !db.Set<WarehouseItem>().Any(w => 
                        db.Set<InboundShipment>().Any(s => s.Id == w.InboundShipmentId && s.ClientOrderCode == code) &&
                        w.Status != WarehouseItemStatus.Inspected &&
                        w.Status != WarehouseItemStatus.Reserved &&
                        w.Status != WarehouseItemStatus.Dispatched &&
                        w.Status != WarehouseItemStatus.Stored
                    )
                    &&
                    db.Set<WarehouseItem>().Any(w => 
                        db.Set<InboundShipment>().Any(s => s.Id == w.InboundShipmentId && s.ClientOrderCode == code) &&
                        w.Status == WarehouseItemStatus.Stored
                    )
                );
            }
            else if (state == PackageStateFilters.Received)
            {
                // HAS at least one WarehouseItem AND NOT (All have WarehouseItem AND All are Inspected/Stored/Reserved/Dispatched)
                matchingKeys = allCodes.Where(code => 
                    db.Set<WarehouseItem>().Any(w => db.Set<InboundShipment>().Any(s => s.Id == w.InboundShipmentId && s.ClientOrderCode == code))
                    &&
                    (
                        db.Set<InboundShipment>().Count(s => s.ClientOrderCode == code) > db.Set<WarehouseItem>().Count(w => db.Set<InboundShipment>().Any(s => s.Id == w.InboundShipmentId && s.ClientOrderCode == code))
                        ||
                        db.Set<WarehouseItem>().Any(w => 
                            db.Set<InboundShipment>().Any(s => s.Id == w.InboundShipmentId && s.ClientOrderCode == code) &&
                            w.Status != WarehouseItemStatus.Inspected &&
                            w.Status != WarehouseItemStatus.Reserved &&
                            w.Status != WarehouseItemStatus.Dispatched &&
                            w.Status != WarehouseItemStatus.Stored
                        )
                    )
                );
            }
            else if (state == PackageStateFilters.Cancelled)
            {
                matchingKeys = allCodes.Where(code => db.Set<InboundShipment>().Where(s => s.ClientOrderCode == code).All(s => s.Status == InboundShipmentStatus.Cancelled));
            }
            else
            {
                matchingKeys = allCodes;
            }

            query = query.Where(s => matchingKeys.Contains(s.ClientOrderCode));
        }

        var groupedQuery = query
            .GroupBy(x => x.ClientOrderCode)
            .Select(g => new PackageGroupProjection
            {
                ClientOrderCode = g.Key,
                CreatedAt = g.Min(s => s.CreatedAt),
                ExpectedArrivalAt = g.Min(s => s.ExpectedArrivalAt),
                FirstReceivedAt = g.Min(s => s.ArrivedAt)
            });

        if (string.IsNullOrWhiteSpace(parameters.SortBy))
        {
            groupedQuery = groupedQuery.OrderByDescending(p => p.CreatedAt);
        }
        else
        {
            groupedQuery = groupedQuery.ApplySort(parameters, InboundShipmentMappings.InboundPackageDtoSortMapping);
        }

        var totalCount = await groupedQuery.CountAsync(cancellationToken);
        
        if (totalCount == 0)
        {
            
            return PagedList<InboundPackageDto>.ToPagedList([], 0, parameters);
        }
        
        var pagedKeys = await groupedQuery
            .Page(parameters)
            .Select(x => x.ClientOrderCode)
            .ToListAsync(cancellationToken);

        var shipments = await db.Set<InboundShipment>()
            .AsNoTracking()
            .Where(s => pagedKeys.Contains(s.ClientOrderCode))
            .ToListAsync(cancellationToken);

        var shipmentIds = shipments.Select(s => s.Id).Distinct().ToList();
        var warehouseItems = await db.Set<WarehouseItem>()
            .AsNoTracking()
            .Where(w => shipmentIds.Contains(w.InboundShipmentId))
            .ToListAsync(cancellationToken);

        var wiByShipment = warehouseItems.ToDictionary(w => w.InboundShipmentId);

        var groups = shipments
            .GroupBy(s => s.ClientOrderCode)
            .Select(g =>
            {
                var first = g.First();
                var siblings = g.ToList();
                var state = PackageStateResolver.Resolve(siblings, wiByShipment);
                var display = PackageStateResolver.ResolveDisplay(siblings, wiByShipment);
                var firstReceivedAt = siblings.Where(x => x.ArrivedAt.HasValue).Min(x => x.ArrivedAt);
                var createdAt = siblings.Min(x => x.CreatedAt);
                return new InboundPackageDto(
                    ClientOrderCode: g.Key,
                    ProviderCode: first.ProviderCode.Id,
                    ShipmentMode: first.ShipmentMode.Id,
                    ExternalCarrierName: first.ExternalCarrierName,
                    CarrierTrackingNumber: first.CarrierTrackingNumber,
                    SenderName: first.SenderName,
                    ExpectedArrivalAt: first.ExpectedArrivalAt,
                    ItemCount: siblings.Count,
                    PackageState: state,
                    FirstReceivedAt: firstReceivedAt,
                    CreatedAt: createdAt,
                    ShippingFee: first.ShippingFee,
                    DisplayStatus: display,
                    CanCancelPackage: PackageStateResolver.CanCancel(siblings, wiByShipment));
            })
            .ToList();

        var finalSortedGroups = pagedKeys.Select(k => groups.First(g => g.ClientOrderCode == k)).ToList();

        return finalSortedGroups.ToPagedList(totalCount, parameters);
    }
}

internal static class PackageStateResolver
{
    public const string PendingArrival = "pending_arrival";
    public const string Received       = "received";
    public const string Stored         = "stored";
    public const string Inspected      = "inspected";

    public static string Resolve(
        IReadOnlyList<InboundShipment> siblings,
        IReadOnlyDictionary<Domain.Context.WarehouseContext.ValueObjects.Ids.InboundShipmentId, WarehouseItem> warehouseItemsByShipment)
    {
        var wis = siblings
            .Select(s => warehouseItemsByShipment.TryGetValue(s.Id, out var w) ? w : null)
            .Where(w => w is not null)
            .Cast<WarehouseItem>()
            .ToList();

        if (wis.Count == siblings.Count && wis.Count > 0 &&
            wis.All(w => w.Status == WarehouseItemStatus.Inspected ||
                         w.Status == WarehouseItemStatus.Reserved ||
                         w.Status == WarehouseItemStatus.Dispatched))
        {
            return Inspected;
        }

        if (wis.Count == siblings.Count && wis.Count > 0 &&
            wis.All(w => w.Status == WarehouseItemStatus.Stored ||
                         w.Status == WarehouseItemStatus.Inspected ||
                         w.Status == WarehouseItemStatus.Reserved ||
                         w.Status == WarehouseItemStatus.Dispatched))
        {
            return Stored;
        }

        if (wis.Count > 0)
            return Received;

        return PendingArrival;
    }

    public static string ResolveDisplay(
        IReadOnlyList<InboundShipment> siblings,
        IReadOnlyDictionary<Domain.Context.WarehouseContext.ValueObjects.Ids.InboundShipmentId, WarehouseItem> warehouseItemsByShipment)
    {
        if (siblings.Count == 0) return "awaiting_pickup";

        if (siblings.All(s => s.Status == InboundShipmentStatus.Cancelled))
            return "cancelled";

        var wis = siblings
            .Select(s => warehouseItemsByShipment.TryGetValue(s.Id, out var w) ? w : null)
            .Where(w => w is not null)
            .Cast<WarehouseItem>()
            .ToList();

        if (wis.Count == siblings.Count && wis.Count > 0 &&
            wis.All(w => w.Status == WarehouseItemStatus.Inspected ||
                         w.Status == WarehouseItemStatus.Reserved ||
                         w.Status == WarehouseItemStatus.Dispatched))
        {
            return "inspected";
        }

        if (wis.Count == siblings.Count && wis.Count > 0 &&
            wis.All(w => w.Status == WarehouseItemStatus.Stored ||
                         w.Status == WarehouseItemStatus.Inspected ||
                         w.Status == WarehouseItemStatus.Reserved ||
                         w.Status == WarehouseItemStatus.Dispatched))
        {
            return "stored";
        }

        if (wis.Count > 0)
            return "received";

        var anyArrived = siblings.Any(s =>
            s.Status == InboundShipmentStatus.Arrived ||
            s.Status == InboundShipmentStatus.Inspected ||
            s.Status == InboundShipmentStatus.Completed);

        if (anyArrived)
            return "arrived";

        if (siblings.Any(s => s.Status == InboundShipmentStatus.SellerClaimsArrived))
            return "seller_claims_arrived";

        if (siblings.Any(s => s.Status == InboundShipmentStatus.InTransit ||
                              s.Status == InboundShipmentStatus.Delivering))
            return "in_transit";

        return "awaiting_pickup";
    }

    /// <summary>
    /// A package is cancelable only when every sibling shipment is still pre-receipt
    /// (awaiting_pickup or in_transit), no WarehouseItem rows exist for any sibling,
    /// and no receipt evidence has been recorded on any sibling.
    /// </summary>
    public static bool CanCancel(
        IReadOnlyList<InboundShipment> siblings,
        IReadOnlyDictionary<Domain.Context.WarehouseContext.ValueObjects.Ids.InboundShipmentId, WarehouseItem> warehouseItemsByShipment)
    {
        if (siblings.Count == 0) return false;

        foreach (var s in siblings)
        {
            if (s.Status != InboundShipmentStatus.AwaitingPickup &&
                s.Status != InboundShipmentStatus.InTransit)
                return false;

            if (warehouseItemsByShipment.ContainsKey(s.Id))
                return false;

            var (media, notes) = ExtractReceipt(s);
            if (media.Count > 0 || !string.IsNullOrWhiteSpace(notes))
                return false;
        }

        return true;
    }

    public static (IReadOnlyList<string> Media, string? Notes) ExtractReceipt(InboundShipment shipment)
    {
        var raw = shipment.ExtraData.RawJson;
        if (string.IsNullOrWhiteSpace(raw) || raw == "{}")
            return (Array.Empty<string>(), null);

        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (!doc.RootElement.TryGetProperty("packageReceipt", out var receipt))
                return (Array.Empty<string>(), null);

            var media = new List<string>();
            if (receipt.TryGetProperty("photos", out var photos) && photos.ValueKind == JsonValueKind.Array)
            {
                foreach (var p in photos.EnumerateArray())
                {
                    var url = p.GetString();
                    if (!string.IsNullOrWhiteSpace(url)) media.Add(url!);
                }
            }

            string? notes = null;
            if (receipt.TryGetProperty("notes", out var n) && n.ValueKind == JsonValueKind.String)
                notes = n.GetString();

            return (media, notes);
        }
        catch
        {
            return (Array.Empty<string>(), null);
        }
    }
}
