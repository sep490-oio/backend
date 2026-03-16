using Microsoft.Extensions.Options;
using OIO.Infrastructure.Scheduling.Jobs;
using Quartz;

namespace OIO.Infrastructure.Scheduling.JobSetup;

public sealed class AuctionAutoCompleteJobSetup : IConfigureOptions<QuartzOptions>
{
    private const string TriggerKey = "auction-auto-complete-trigger";
    private static readonly JobKey JobKey = new("auction-auto-complete", JobConstants.SystemGroup);

    public void Configure(QuartzOptions options)
    {
        options.AddJob<AuctionAutoCompleteJob>(opts => opts
            .WithIdentity(JobKey)
            .StoreDurably());
        options.AddTrigger(opts => opts
            .ForJob(JobKey)
            .WithIdentity(TriggerKey, JobKey.Group)
            .WithSimpleSchedule(x => x
                .WithIntervalInHours(1)
                .RepeatForever()
                .WithMisfireHandlingInstructionFireNow()));
    }
}
