using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;

namespace OIO.Application.Abstractions.Search;

public interface IElasticsearchSyncService
{
    Task SyncItemAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task SyncAuctionAsync(Guid auctionId, CancellationToken cancellationToken = default);
    Task SyncUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SyncOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task SyncShipmentAsync(Guid shipmentId, bool isOutbound, CancellationToken cancellationToken = default);
    Task SyncWarehouseItemAsync(Guid warehouseItemId, CancellationToken cancellationToken = default);
    Task SyncAllAsync(CancellationToken cancellationToken = default);
    Task ClearCacheAsync(CancellationToken cancellationToken = default);
}
