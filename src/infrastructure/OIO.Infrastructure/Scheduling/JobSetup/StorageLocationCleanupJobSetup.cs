using Microsoft.Extensions.Options;
using OIO.Infrastructure.Scheduling.Jobs.Warehouses;
using Quartz;

namespace OIO.Infrastructure.Scheduling.JobSetup;

/// <summary>
/// Schedules <see cref="StorageLocationCleanupJob"/> every hour.
/// Cron: <c>0 0 * * * ?</c>.
/// </summary>
internal sealed class StorageLocationCleanupJobSetup : IConfigureOptions<QuartzOptions>
{
    public void Configure(QuartzOptions options)
    {
        var jobKey = JobKey.Create(nameof(StorageLocationCleanupJob));

        options.AddJob<StorageLocationCleanupJob>(builder =>
            builder.WithIdentity(jobKey));

        options.AddTrigger(trigger =>
            trigger
                .ForJob(jobKey)
                .WithIdentity($"{nameof(StorageLocationCleanupJob)}-trigger")
                .WithCronSchedule("0 0 * * * ?"));
    }
}
