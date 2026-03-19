using Microsoft.Extensions.Options;
using OIO.Infrastructure.Scheduling.Jobs;
using Quartz;

namespace OIO.Infrastructure.Scheduling.JobSetup;

public sealed class RecalculateSellerTrustScoresJobSetup : IConfigureOptions<QuartzOptions>
{
    private const string TriggerKey = "recalculate-seller-trust-scores-trigger";
    private static readonly JobKey JobKey = new("recalculate-seller-trust-scores", JobConstants.SystemGroup);

    public void Configure(QuartzOptions options)
    {
        options.AddJob<RecalculateSellerTrustScoresJob>(opts => opts
            .WithIdentity(JobKey)
            .StoreDurably());
        options.AddTrigger(opts => opts
            .ForJob(JobKey)
            .WithIdentity(TriggerKey, JobKey.Group)
            .WithSimpleSchedule(x => x
                .WithIntervalInHours(6)
                .RepeatForever()
                .WithMisfireHandlingInstructionFireNow()));
    }
}
