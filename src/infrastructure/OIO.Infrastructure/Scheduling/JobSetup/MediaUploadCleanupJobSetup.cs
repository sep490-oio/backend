using Microsoft.Extensions.Options;
using OIO.Infrastructure.Scheduling.Jobs;
using Quartz;

namespace OIO.Infrastructure.Scheduling.JobSetup;

public sealed class MediaUploadCleanupJobSetup : IConfigureOptions<QuartzOptions>
{
    private const string CleanupJobTriggerKey = "pending-upload-cleanup-trigger";
    private static readonly JobKey CleanupJobKey = new("pending-upload-cleanup", JobConstants.SystemGroup);

    public void Configure(QuartzOptions options)
    {
        options.AddJob<PendingUploadCleanupJob>(opts => opts
            .WithIdentity(CleanupJobKey)
            .StoreDurably());
        options.AddTrigger(opts => opts
            .ForJob(CleanupJobKey)
            .WithIdentity(CleanupJobTriggerKey, CleanupJobKey.Group)
            .WithSimpleSchedule(x => x
                .WithIntervalInMinutes(15)
                .RepeatForever()
                .WithMisfireHandlingInstructionFireNow()));
    }
}