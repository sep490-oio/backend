using Microsoft.Extensions.Options;
using OIO.Infrastructure.Scheduling.Jobs;
using OIO.Infrastructure.Scheduling.Jobs.Auctions;
using Quartz;

namespace OIO.Infrastructure.Scheduling.JobSetup;

public class AuctionJobSetup : IConfigureOptions<QuartzOptions>
{
    private const string FallbackJobTriggerIdentity = "auction-polling-fallback-trigger";
    private static readonly JobKey FallbackJobKey = new("auction-polling-fallback", JobConstants.SystemGroup);
    
    public void Configure(QuartzOptions options)
    {
        options.AddJob<AuctionPollingFallbackJob>(opts => opts
            .WithIdentity(FallbackJobKey)
            .StoreDurably());
        
        options.AddTrigger(opts => opts
            .ForJob(FallbackJobKey)
            .WithIdentity(FallbackJobTriggerIdentity, FallbackJobKey.Group)
            .WithSimpleSchedule(x => x
                .WithIntervalInSeconds(60)
                .RepeatForever()
                .WithMisfireHandlingInstructionFireNow()));
    }
}