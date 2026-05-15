using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.OrderContext.Aggregates.SellerDirectShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.Admins.GetAdminItemLogistics;

public sealed record GetAdminItemLogisticsQuery(Guid ItemId) : IQuery<List<AdminItemLogisticsEventDto>>;

internal sealed class GetAdminItemLogisticsQueryHandler
    : IQueryHandler<GetAdminItemLogisticsQuery, List<AdminItemLogisticsEventDto>>
{
    private readonly IDbContext _dbContext;

    public GetAdminItemLogisticsQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<List<AdminItemLogisticsEventDto>, Error>> Handle(
        GetAdminItemLogisticsQuery request,
        CancellationToken cancellationToken)
    {
        var events = new List<AdminItemLogisticsEventDto>();

        // 1. Get Inbound Shipments
        var inboundShipments = await _dbContext.Set<InboundShipment>()
            .AsNoTracking()
            .Where(x => x.ItemId == request.ItemId)
            .ToListAsync(cancellationToken);

        foreach (var inbound in inboundShipments)
        {
            events.Add(new AdminItemLogisticsEventDto(
                EventType: "INBOUND_" + inbound.Status.Id.ToUpper(),
                Timestamp: inbound.ModifiedAt ?? inbound.CreatedAt,
                Description: $"Inbound Shipment to Warehouse: {inbound.Status.Id}",
                Location: "Warehouse",
                Carrier: inbound.ProviderCode.Id,
                TrackingCode: inbound.CarrierTrackingNumber,
                ReferenceId: inbound.Id.Value.ToString()
            ));
        }

        // 2. Get Warehouse Items
        var warehouseItems = await _dbContext.Set<WarehouseItem>()
            .AsNoTracking()
            .Where(x => x.ItemId == request.ItemId)
            .ToListAsync(cancellationToken);

        var locationIds = warehouseItems
            .Where(x => x.StorageLocationId != null)
            .Select(x => x.StorageLocationId!.Value)
            .Distinct()
            .ToList();

        var locations = await _dbContext.Set<OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage.WarehouseStorageLocation>()
            .AsNoTracking()
            .Where(l => locationIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => l.Label, cancellationToken);

        foreach (var wi in warehouseItems)
        {
            events.Add(new AdminItemLogisticsEventDto(
                EventType: "WAREHOUSE_" + wi.Status.Id.ToUpper(),
                Timestamp: wi.ModifiedAt ?? wi.CreatedAt,
                Description: $"Warehouse Item: {wi.Status.Id}",
                Location: wi.StorageLocationId != null && locations.TryGetValue(wi.StorageLocationId.Value, out var label) ? label : (wi.StorageLocationId != null ? $"Bin: {wi.StorageLocationId}" : "Warehouse"),
                Carrier: null,
                TrackingCode: null,
                ReferenceId: wi.Id.Value.ToString()
            ));

            // 3. Get Outbound Shipments related to this Warehouse Item
            var outboundShipments = await _dbContext.Set<OutboundShipment>()
                .AsNoTracking()
                .Where(x => x.WarehouseItemId == wi.Id)
                .ToListAsync(cancellationToken);

            foreach (var outbound in outboundShipments)
            {
                events.Add(new AdminItemLogisticsEventDto(
                    EventType: "OUTBOUND_" + outbound.Status.Id.ToUpper(),
                    Timestamp: outbound.ModifiedAt ?? outbound.CreatedAt,
                    Description: $"Outbound Shipment from Warehouse: {outbound.Status.Id}",
                    Location: null,
                    Carrier: outbound.ProviderCode.Id,
                    TrackingCode: outbound.CarrierTrackingNumber,
                    ReferenceId: outbound.Id.Value.ToString()
                ));
            }
        }

        // 4. Get Direct Shipments
        // Since DirectShipments use OrderId, we need to find OrderId by ItemId.
        // ItemId -> AuctionId -> OrderId
        var targetItemId = ItemId.From(request.ItemId);
        var auctionIds = await _dbContext.Set<Domain.Context.AuctionContext.Aggregates.Auctions.Auction>()
            .AsNoTracking()
            .Where(a => a.ItemId == targetItemId)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        if (auctionIds.Count > 0)
        {
            var orderIds = await _dbContext.Set<Domain.Context.OrderContext.Aggregates.Orders.Order>()
                .AsNoTracking()
                .Where(o => auctionIds.Contains(o.AuctionId))
                .Select(o => o.Id)
                .ToListAsync(cancellationToken);

            if (orderIds.Count > 0)
            {
                var directShipments = await _dbContext.Set<SellerDirectShipment>()
                    .AsNoTracking()
                    .Where(x => orderIds.Contains(x.OrderId))
                    .ToListAsync(cancellationToken);

                foreach (var ds in directShipments)
                {
                    events.Add(new AdminItemLogisticsEventDto(
                        EventType: "DIRECT_" + ds.Status.Id.ToUpper(),
                        Timestamp: ds.ModifiedAt ?? ds.CreatedAt,
                        Description: $"Direct Shipment: {ds.Status.Id}",
                        Location: "Seller Direct",
                        Carrier: ds.ExternalCarrierName,
                        TrackingCode: ds.ExternalTrackingCode,
                        ReferenceId: ds.Id.Value.ToString()
                    ));
                }
            }
        }

        // Order by timestamp
        var sortedEvents = events.OrderByDescending(x => x.Timestamp).ToList();

        return sortedEvents;
    }
}
