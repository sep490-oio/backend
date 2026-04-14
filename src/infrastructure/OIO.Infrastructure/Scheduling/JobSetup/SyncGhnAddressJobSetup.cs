using Microsoft.Extensions.Options;
using OIO.Infrastructure.Scheduling.Jobs;
using Quartz;

namespace OIO.Infrastructure.Scheduling.JobSetup;

public sealed class SyncGhnAddressJobSetup : IConfigureOptions<QuartzOptions>
{
    private const string TriggerKey = "sync-ghn-address-trigger";
    private static readonly JobKey JobKey = new("sync-ghn-address", JobConstants.SystemGroup);

    public void Configure(QuartzOptions options)
    {
        options.AddJob<SyncGhnAddressJob>(opts => opts
            .WithIdentity(JobKey)
            .StoreDurably());

        // Cron: "0 0 1 ? * SUN *" -> Every Sunday at 1:00 AM
        options.AddTrigger(opts => opts
            .ForJob(JobKey)
            .WithIdentity(TriggerKey, JobKey.Group)
            .WithCronSchedule("0 0 1 ? * SUN *", x => x
                .WithMisfireHandlingInstructionFireAndProceed()));
    }
}
