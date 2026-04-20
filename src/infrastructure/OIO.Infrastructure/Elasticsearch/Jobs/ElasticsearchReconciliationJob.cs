using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Search;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using Quartz;

namespace OIO.Infrastructure.Elasticsearch.Jobs;

[DisallowConcurrentExecution]
public class ElasticsearchReconciliationJob(
    IDbContext dbContext,
    IElasticsearchSyncService syncService) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        // This job is a safety net to ensure DB and ES are in sync.
        // It performs a simplified bootstrap for all entities.
        
        // Sync Items
        var itemIds = await dbContext.Set<Item>().Select(i => i.Id.Value).ToListAsync(context.CancellationToken);
        foreach (var id in itemIds) await syncService.SyncItemAsync(id, context.CancellationToken);
        
        // Sync Auctions
        var auctionIds = await dbContext.Set<Auction>().Select(a => a.Id.Value).ToListAsync(context.CancellationToken);
        foreach (var id in auctionIds) await syncService.SyncAuctionAsync(id, context.CancellationToken);
        
        // Sync Users
        var userIds = await dbContext.Set<User>().Select(u => u.Id.Value).ToListAsync(context.CancellationToken);
        foreach (var id in userIds) await syncService.SyncUserAsync(id, context.CancellationToken);
        
        // Sync Orders
        var orderIds = await dbContext.Set<Order>().Select(o => o.Id.Value).ToListAsync(context.CancellationToken);
        foreach (var id in orderIds) await syncService.SyncOrderAsync(id, context.CancellationToken);
        
        // Sync Shipments
        var inShipmentIds = await dbContext.Set<InboundShipment>().Select(s => s.Id.Value).ToListAsync(context.CancellationToken);
        foreach (var id in inShipmentIds) await syncService.SyncShipmentAsync(id, false, context.CancellationToken);
        
        var outShipmentIds = await dbContext.Set<OutboundShipment>().Select(s => s.Id.Value).ToListAsync(context.CancellationToken);
        foreach (var id in outShipmentIds) await syncService.SyncShipmentAsync(id, true, context.CancellationToken);

        await syncService.ClearCacheAsync(context.CancellationToken);
    }
}
