using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.NotificationContext.Services;
using OIO.Domain.Context.NotificationContext.Aggregates;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Infrastructure.Persistence;
using Quartz;

namespace OIO.Infrastructure.Notification.BackgroundJobs;

[DisallowConcurrentExecution]
internal sealed class ProcessNotificationDeliveriesJob : IJob
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEnumerable<INotificationProvider> _providers;
    private readonly ILogger<ProcessNotificationDeliveriesJob> _logger;

    public ProcessNotificationDeliveriesJob(
        ApplicationDbContext dbContext,
        IEnumerable<INotificationProvider> providers,
        ILogger<ProcessNotificationDeliveriesJob> logger)
    {
        _dbContext = dbContext;
        _providers = providers;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        var now = DateTime.UtcNow;

        var pendingDeliveries = await _dbContext.Set<NotificationDelivery>()
            .Include(d => d.Notification)
            .Where(d => (d.Status == NotificationDeliveryStatus.Pending && d.ScheduledAt <= now) ||
                        (d.Status == NotificationDeliveryStatus.Failed && d.NextRetryAt <= now && d.AttemptCount < d.MaxAttempts))
            .OrderBy(d => d.ScheduledAt)
            .Take(50)
            .ToListAsync(ct);

        if (!pendingDeliveries.Any())
            return;

        foreach (var delivery in pendingDeliveries)
        {
            var provider = _providers.FirstOrDefault(p =>
                p.ChannelType.Equals(delivery.Channel.Id, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
            {
                _logger.LogWarning("No provider found for channel {Channel}", delivery.Channel.Id);
                delivery.MarkAsFailed("NO_PROVIDER", $"No provider configured for channel {delivery.Channel.Id}", now);
                continue;
            }

            try
            {
                var result = await provider.SendAsync(delivery.Notification, delivery, ct);
                
                if (result.IsSuccess)
                {
                    delivery.MarkAsSent(now);
                }
                else
                {
                    delivery.MarkAsFailed("SEND_ERROR", result.Error, now);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception delivering notification {Id}", delivery.Id.Value);
                delivery.MarkAsFailed("EXCEPTION", ex.Message, now);
            }
        }

        await _dbContext.SaveChangesAsync(ct);
    }
}
