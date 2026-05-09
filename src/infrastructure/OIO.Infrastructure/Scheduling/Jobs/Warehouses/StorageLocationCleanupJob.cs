using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Infrastructure.Persistence;
using Quartz;

namespace OIO.Infrastructure.Scheduling.Jobs.Warehouses;

[DisallowConcurrentExecution]
internal sealed class StorageLocationCleanupJob(
    ApplicationDbContext dbContext,
    ILogger<StorageLocationCleanupJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Starting periodic storage location cleanup job.");

        // Find all locations that are marked occupied but have no WarehouseItems assigned
        var orphanedLocations = await dbContext.Set<WarehouseStorageLocation>()
            .Where(l => l.IsOccupied && !dbContext.Set<WarehouseItem>().Any(wi => wi.StorageLocationId == l.Id))
            .ToListAsync(context.CancellationToken);

        if (orphanedLocations.Count == 0)
        {
            logger.LogInformation("No orphaned storage locations found.");
            return;
        }

        foreach (var location in orphanedLocations)
        {
            location.MarkVacant();
            logger.LogInformation("Marked orphaned storage location {LocationId} ({Label}) as vacant.", location.Id.Value, location.Label);
        }

        dbContext.UpdateRange(orphanedLocations);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Storage location cleanup job completed. {Count} locations vacated.", orphanedLocations.Count);
    }
}
