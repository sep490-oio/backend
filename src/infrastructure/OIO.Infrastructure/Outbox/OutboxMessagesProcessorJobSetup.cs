using Microsoft.Extensions.Options;
using OIO.Infrastructure.Scheduling;
using OIO.Infrastructure.Settings;
using Quartz;

namespace OIO.Infrastructure.Outbox;

internal sealed class OutboxMessagesProcessorJobSetup(
    IOptions<OutboxSettings> outboxSettingsOptions) : IConfigureOptions<QuartzOptions>
{
    private const string CleanupJobTriggerIdentity = "outbox-cleanup-trigger";
    private static readonly JobKey CleanupJobKey = new("outbox-cleanup", JobConstants.SystemGroup);

    private const string ProcessJobTriggerIdentity = "outbox-process-trigger";
    private static readonly JobKey ProcessJobKey = new("outbox-process", JobConstants.SystemGroup);

    public void Configure(QuartzOptions options)
    {
        var outboxSettings = outboxSettingsOptions.Value;

        options.AddJob<OutboxMessagesProcessorJob>(jobBuilder => 
                jobBuilder
                    .WithIdentity(ProcessJobKey)
                    .StoreDurably())
            .AddTrigger(trigger =>
                trigger
                    .ForJob(ProcessJobKey)
                    .WithIdentity(ProcessJobTriggerIdentity, ProcessJobKey.Group)
                    .WithSimpleSchedule(scheduleBuilder =>
                        scheduleBuilder
                            .WithInterval(outboxSettings.Interval)
                            .RepeatForever()));

        // Cleanup job - daily at 03:00 (can be overridden later if needed)
        options.AddJob<OutboxCleanupJob>(jobBuilder => 
                jobBuilder
                    .WithIdentity(CleanupJobKey)
                    .StoreDurably())
            .AddTrigger(trigger =>
                trigger
                    .ForJob(CleanupJobKey)
                    .WithIdentity(CleanupJobTriggerIdentity, CleanupJobKey.Group)
                    .WithCronSchedule("0 0 3 * * ?"));

        // 0 0 3 * * ? (Quartz cron) triggers at 03:00:00 every day. Breakdown of the fields:
        // Seconds: 0
        // Minutes: 0
        // Hours: 3
        // Day-of-month: * (every day)
        // Month: * (every month)
        // Day-of-week: ? (no specific value; used as a placeholder because day-of-month is used)
        // Year: omitted (every year)
        // So the job runs daily at 3:00 AM (Quartz uses the scheduler's configured timezone by default).
    }
}