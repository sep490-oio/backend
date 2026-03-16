using Microsoft.Extensions.Options;
using OIO.Infrastructure.Scheduling.Jobs;
using Quartz;

namespace OIO.Infrastructure.Scheduling.JobSetup;

public sealed class PendingUploadRelocationJobSetup : IConfigureOptions<QuartzOptions>
{
    private const string TriggerKey = "pending-upload-relocation-trigger";
    private static readonly JobKey JobKey = new("pending-upload-relocation", JobConstants.SystemGroup);

    public void Configure(QuartzOptions options)
    {
        options.AddJob<PendingUploadRelocationJob>(opts => opts
            .WithIdentity(JobKey)
            .StoreDurably());
        options.AddTrigger(opts => opts
            .ForJob(JobKey)
            .WithIdentity(TriggerKey, JobKey.Group)
            .WithSimpleSchedule(x => x
                .WithIntervalInMinutes(15)
                .RepeatForever()
                .WithMisfireHandlingInstructionFireNow()));
    }
}
