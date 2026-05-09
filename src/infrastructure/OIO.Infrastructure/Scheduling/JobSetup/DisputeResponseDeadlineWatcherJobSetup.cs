using Microsoft.Extensions.Options;
using OIO.Infrastructure.Scheduling.Jobs.Disputes;
using Quartz;

namespace OIO.Infrastructure.Scheduling.JobSetup;

/// <summary>
/// Schedules <see cref="DisputeResponseDeadlineWatcherJob"/> every hour.
/// Cron: <c>0 0 * * * ?</c> (top of every hour).
/// </summary>
internal sealed class DisputeResponseDeadlineWatcherJobSetup : IConfigureOptions<QuartzOptions>
{
    public void Configure(QuartzOptions options)
    {
        var jobKey = JobKey.Create(nameof(DisputeResponseDeadlineWatcherJob));

        options.AddJob<DisputeResponseDeadlineWatcherJob>(builder =>
            builder.WithIdentity(jobKey));

        options.AddTrigger(trigger =>
            trigger
                .ForJob(jobKey)
                .WithIdentity($"{nameof(DisputeResponseDeadlineWatcherJob)}-trigger")
                .WithCronSchedule("0 0 * * * ?"));
    }
}
