using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace OIO.Infrastructure.Outbox;

[DisallowConcurrentExecution]
internal sealed class OutboxMessagesProcessorJob(
    ILogger<OutboxMessagesProcessorJob> logger,
    IServiceScopeFactory scopeFactory
) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;

        using var scope = scopeFactory.CreateScope();
        var outboxProcessor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();

        try
        {
            await outboxProcessor.Execute(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            OutboxMessagesProcessorLoggers.LogCancelled(logger);
            throw;
        }
        catch (Exception ex)
        {
            OutboxMessagesProcessorLoggers.LogError(logger, ex);
            throw new JobExecutionException(cause: ex, refireImmediately: true);
        }
    }
}
