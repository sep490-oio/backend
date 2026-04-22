using Microsoft.Extensions.Options;
using OIO.Infrastructure.Scheduling.Jobs.Orders;
using Quartz;

namespace OIO.Infrastructure.Scheduling.JobSetup;

/// <summary>
/// Schedules <see cref="OrderReturnAutoConfirmJob"/> at 03:15 UTC daily.
/// Offset 15 minutes from <see cref="WarehouseReturnAutoConfirmJobSetup"/>
/// (03:00 UTC) and 1h 15m from <see cref="OrderReturnDeadlineWatcherJobSetup"/>
/// (02:00 UTC) to avoid simultaneous DB-contention spikes. Cron: <c>0 15 3 * * ?</c>.
/// </summary>
internal sealed class OrderReturnAutoConfirmJobSetup : IConfigureOptions<QuartzOptions>
{
    public void Configure(QuartzOptions options)
    {
        var jobKey = JobKey.Create(nameof(OrderReturnAutoConfirmJob));

        options.AddJob<OrderReturnAutoConfirmJob>(builder =>
            builder.WithIdentity(jobKey));

        options.AddTrigger(trigger =>
            trigger
                .ForJob(jobKey)
                .WithIdentity($"{nameof(OrderReturnAutoConfirmJob)}-trigger")
                .WithCronSchedule("0 15 3 * * ?"));
    }
}
