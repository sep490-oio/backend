using Microsoft.Extensions.Options;
using OIO.Infrastructure.Scheduling.Jobs.Warehouses;
using Quartz;

namespace OIO.Infrastructure.Scheduling.JobSetup;

/// <summary>
/// Schedules <see cref="WarehouseReturnAutoConfirmJob"/> at 03:00 UTC daily.
/// Offset 1h from <see cref="OrderReturnDeadlineWatcherJob"/>'s 02:00 UTC slot
/// to avoid DB-contention spikes. Cron: <c>0 0 3 * * ?</c>.
/// </summary>
internal sealed class WarehouseReturnAutoConfirmJobSetup : IConfigureOptions<QuartzOptions>
{
    public void Configure(QuartzOptions options)
    {
        var jobKey = JobKey.Create(nameof(WarehouseReturnAutoConfirmJob));

        options.AddJob<WarehouseReturnAutoConfirmJob>(builder =>
            builder.WithIdentity(jobKey));

        options.AddTrigger(trigger =>
            trigger
                .ForJob(jobKey)
                .WithIdentity($"{nameof(WarehouseReturnAutoConfirmJob)}-trigger")
                .WithCronSchedule("0 0 3 * * ?"));
    }
}
