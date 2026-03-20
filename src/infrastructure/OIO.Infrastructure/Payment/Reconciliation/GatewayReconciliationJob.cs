using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Context.PaymentContext.Commands.ReconcileTransactions;
using OIO.Infrastructure.Scheduling;
using Quartz;

namespace OIO.Infrastructure.Payment.Reconciliation;

[DisallowConcurrentExecution]
internal sealed class GatewayReconciliationJob(
    ILogger<GatewayReconciliationJob> logger,
    IServiceScopeFactory scopeFactory
) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;

        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        try
        {
            logger.LogInformation("Starting Gateway Reconciliation Job...");
            // Process up to 100 transactions per batch
            await mediator.Send(new ProcessGatewayReconciliationCommand(100), cancellationToken);
            logger.LogInformation("Gateway Reconciliation Job finished.");
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("GatewayReconciliationJob cancelled.");
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
