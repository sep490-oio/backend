using Microsoft.Extensions.Options;
using OIO.Infrastructure.Scheduling.Jobs.Orders;
using Quartz;

namespace OIO.Infrastructure.Scheduling.JobSetup;

/// <summary>
/// Schedules <see cref="OrderReturnReminderJob"/> hourly. Matches the cadence
/// of <see cref="OrderAutoCompleteJobSetup"/> (WithIntervalInHours(1)) so the
/// runtime surface is uniform with existing jobs.
/// </summary>
internal sealed class OrderReturnReminderJobSetup : IConfigureOptions<QuartzOptions>
{
    public void Configure(QuartzOptions options)
    {
        var jobKey = JobKey.Create(nameof(OrderReturnReminderJob));

        options.AddJob<OrderReturnReminderJob>(builder =>
            builder.WithIdentity(jobKey));

        options.AddTrigger(trigger =>
            trigger
                .ForJob(jobKey)
                .WithIdentity($"{nameof(OrderReturnReminderJob)}-trigger")
                .WithSimpleSchedule(schedule =>
                    schedule.WithIntervalInHours(1).RepeatForever()));
    }
}
