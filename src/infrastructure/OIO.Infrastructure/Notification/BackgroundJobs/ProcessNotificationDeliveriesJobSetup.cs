using Microsoft.Extensions.Options;
using Quartz;

namespace OIO.Infrastructure.Notification.BackgroundJobs;

internal sealed class ProcessNotificationDeliveriesJobSetup : IConfigureOptions<QuartzOptions>
{
    public void Configure(QuartzOptions options)
    {
        var jobKey = JobKey.Create(nameof(ProcessNotificationDeliveriesJob));

        options
            .AddJob<ProcessNotificationDeliveriesJob>(jobBuilder => jobBuilder.WithIdentity(jobKey))
            .AddTrigger(trigger => trigger
                .ForJob(jobKey)
                .WithSimpleSchedule(schedule =>
                    schedule.WithIntervalInSeconds(10).RepeatForever()));
    }
}
