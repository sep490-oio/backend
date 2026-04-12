using Microsoft.Extensions.Options;
using Quartz;

namespace OIO.Infrastructure.Elasticsearch.Jobs;

public class ElasticsearchReconciliationJobSetup : IConfigureOptions<QuartzOptions>
{
    public void Configure(QuartzOptions options)
    {
        var jobKey = new JobKey(nameof(ElasticsearchReconciliationJob));

        options.AddJob<ElasticsearchReconciliationJob>(jobBuilder => jobBuilder.WithIdentity(jobKey))
               .AddTrigger(trigger =>
                   trigger
                       .ForJob(jobKey)
                       // Run every 24 hours at 2 AM
                       .WithCronSchedule("0 0 2 * * ?"));
    }
}
