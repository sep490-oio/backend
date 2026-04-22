using Microsoft.Extensions.Options;
using OIO.Infrastructure.Scheduling.Jobs.Orders;
using Quartz;

namespace OIO.Infrastructure.Scheduling.JobSetup;

/// <summary>
/// Schedules <see cref="OrderReturnDeadlineWatcherJob"/> at 02:00 UTC daily
/// (09:00 Hanoi). Cron: <c>0 0 2 * * ?</c>.
/// </summary>
internal sealed class OrderReturnDeadlineWatcherJobSetup : IConfigureOptions<QuartzOptions>
{
    public void Configure(QuartzOptions options)
    {
        var jobKey = JobKey.Create(nameof(OrderReturnDeadlineWatcherJob));

        options.AddJob<OrderReturnDeadlineWatcherJob>(builder =>
            builder.WithIdentity(jobKey));

        options.AddTrigger(trigger =>
            trigger
                .ForJob(jobKey)
                .WithIdentity($"{nameof(OrderReturnDeadlineWatcherJob)}-trigger")
                .WithCronSchedule("0 0 2 * * ?"));
    }
}
