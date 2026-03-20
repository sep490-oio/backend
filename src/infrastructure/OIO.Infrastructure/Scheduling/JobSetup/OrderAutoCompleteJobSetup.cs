using Microsoft.Extensions.Options;
using OIO.Infrastructure.Scheduling.Jobs;
using Quartz;

namespace OIO.Infrastructure.Scheduling.JobSetup;

internal sealed class OrderAutoCompleteJobSetup : IConfigureOptions<QuartzOptions>
{
    public void Configure(QuartzOptions options)
    {
        var jobKey = JobKey.Create(nameof(OrderAutoCompleteJob));

        options.AddJob<OrderAutoCompleteJob>(jobBuilder =>
            jobBuilder.WithIdentity(jobKey));

        // Let's run it once every hour to check for expired 3-day cooling periods
        options.AddTrigger(triggerBuilder =>
            triggerBuilder
                .ForJob(jobKey)
                .WithSimpleSchedule(schedule =>
                    schedule.WithIntervalInHours(1).RepeatForever()));
    }
}
