using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Infrastructure.Scheduling;
using Quartz;

namespace OIO.Infrastructure.Payment.Webhooks;

[DisallowConcurrentExecution]
internal sealed class ProcessGatewayWebhooksJob(
    ILogger<ProcessGatewayWebhooksJob> logger,
    IServiceScopeFactory scopeFactory
) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;

        using var scope = scopeFactory.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<GatewayWebhookProcessor>();

        try
        {
            // Process up to 50 webhooks per batch
            await processor.ProcessAsync(50, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("ProcessGatewayWebhooksJob cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while processing gateway webhooks");
            throw new JobExecutionException(cause: ex, refireImmediately: true);
        }
    }
}

internal sealed class ProcessGatewayWebhooksJobSetup : IConfigureOptions<QuartzOptions>
{
    private const string ProcessJobTriggerIdentity = "gateway-webhooks-process-trigger";
    private static readonly JobKey ProcessJobKey = new("gateway-webhooks-process", JobConstants.SystemGroup);

    public void Configure(QuartzOptions options)
    {
        options.AddJob<ProcessGatewayWebhooksJob>(jobBuilder => 
                jobBuilder
                    .WithIdentity(ProcessJobKey)
                    .StoreDurably())
            .AddTrigger(trigger =>
                trigger
                    .ForJob(ProcessJobKey)
                    .WithIdentity(ProcessJobTriggerIdentity, ProcessJobKey.Group)
                    .WithSimpleSchedule(scheduleBuilder =>
                        scheduleBuilder
                            .WithIntervalInSeconds(10) // Quét mỗi 10 giây để xử lý webhooks realtime nhất có thể
                            .RepeatForever()));
    }
}
