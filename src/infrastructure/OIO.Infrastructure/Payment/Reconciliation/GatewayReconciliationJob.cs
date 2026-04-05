using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.PaymentContext.Commands.ReconcileTransactions;
using OIO.Infrastructure.Scheduling;
using Quartz;

namespace OIO.Infrastructure.Payment.Reconciliation;

[DisallowConcurrentExecution]
internal sealed class GatewayReconciliationJob(
    ILogger<GatewayReconciliationJob> logger,
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<AppLoggingOptions> loggingOptions
) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var cancellationToken = context.CancellationToken;

        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        try
        {
            await mediator.Send(new ProcessGatewayReconciliationCommand(100), cancellationToken);
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds >= loggingOptions.CurrentValue.Jobs.SlowJobThresholdMs)
            {
                logger.LogWarning(
                    "GatewayReconciliationJob completed in {DurationMs}ms.",
                    stopwatch.ElapsedMilliseconds);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("GatewayReconciliationJob cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while executing GatewayReconciliationJob");
            throw new JobExecutionException(cause: ex, refireImmediately: true);
        }
    }
}

internal sealed class GatewayReconciliationJobSetup : IConfigureOptions<QuartzOptions>
{
    private const string ProcessJobTriggerIdentity = "gateway-reconciliation-process-trigger";
    private static readonly JobKey ProcessJobKey = new("gateway-reconciliation-process", JobConstants.SystemGroup);

    public void Configure(QuartzOptions options)
    {
        options.AddJob<GatewayReconciliationJob>(jobBuilder => 
                jobBuilder
                    .WithIdentity(ProcessJobKey)
                    .StoreDurably())
            .AddTrigger(trigger =>
                trigger
                    .ForJob(ProcessJobKey)
                    .WithIdentity(ProcessJobTriggerIdentity, ProcessJobKey.Group)
                    .WithSimpleSchedule(scheduleBuilder =>
                        scheduleBuilder
                            .WithIntervalInMinutes(15) // Quét mỗi 15 phút
                            .RepeatForever()));
    }
}
