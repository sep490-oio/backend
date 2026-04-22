using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using MediatR;
using Quartz;

namespace OIO.Infrastructure.Scheduling.Jobs.Orders;

/// <summary>
/// Hourly sweep that reminds buyers whose <see cref="OrderReturn"/> deadline is
/// 3 days or 1 day out. Dedups via <c>LastReminderSentAt</c> (set after send).
/// Per plan E3, we use day-granularity targeting (<c>daysUntilDue</c> ∈ {3,1})
/// plus a 20-hour cooldown on <c>LastReminderSentAt</c> so consecutive hourly
/// runs don't double-send.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class OrderReturnReminderJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClock _clock;
    private readonly IOptionsMonitor<AppLoggingOptions> _loggingOptions;
    private readonly ILogger<OrderReturnReminderJob> _logger;

    public OrderReturnReminderJob(
        IServiceScopeFactory scopeFactory,
        IClock clock,
        IOptionsMonitor<AppLoggingOptions> loggingOptions,
        ILogger<OrderReturnReminderJob> logger)
    {
        _scopeFactory   = scopeFactory;
        _clock          = clock;
        _loggingOptions = loggingOptions;
        _logger         = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        using var scope = _scopeFactory.CreateScope();
        var dbContext  = scope.ServiceProvider.GetRequiredService<IDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var sender     = scope.ServiceProvider.GetRequiredService<ISender>();

        var ct  = context.CancellationToken;
        var now = _clock.UtcNow;

        // Fetch approved, unshipped, still-open returns whose deadline is within
        // the 3-day horizon. We compute daysUntilDue in-memory (no computed prop
        // in Where/OrderBy — constraint compliance).
        var horizon = now.AddDays(3).AddHours(1); // +1h slack to catch drift
        var cooldownCutoff = now.AddHours(-20);

        var candidates = await dbContext.Set<OrderReturn>()
            .Where(r => r.Status == OrderReturnStatus.Approved
                     && r.ShippedAt == null
                     && r.BuyerDecisionDueAt != null
                     && r.BuyerDecisionDueAt > now
                     && r.BuyerDecisionDueAt <= horizon
                     && (r.LastReminderSentAt == null || r.LastReminderSentAt < cooldownCutoff))
            .Take(500)
            .ToListAsync(ct);

        var sentCount = 0;
        var failedCount = 0;

        foreach (var orderReturn in candidates)
        {
            if (!orderReturn.BuyerDecisionDueAt.HasValue)
                continue;

            var daysUntilDue = (int)Math.Floor((orderReturn.BuyerDecisionDueAt.Value - now).TotalDays);
            if (daysUntilDue != 3 && daysUntilDue != 1)
                continue;

            try
            {
                var notificationResult = await sender.Send(
                    new CreateNotificationCommand(
                        UserId:           orderReturn.BuyerId.Value,
                        NotificationType: "order_return",
                        EventType:        $"order_return_reminder_{daysUntilDue}d",
                        Title:            daysUntilDue == 3
                            ? "Nhac nho: con 3 ngay de gui tra hang"
                            : "Sap het han: con 1 ngay de gui tra hang",
                        Message:          $"Ban con {daysUntilDue} ngay de gui tra hang cho don hang. Vui long hoan tat viec gui tra truoc ngay {orderReturn.BuyerDecisionDueAt:yyyy-MM-dd}.",
                        Priority:         daysUntilDue == 1
                            ? NotificationPriority.High
                            : NotificationPriority.Normal,
                        EntityType:       "OrderReturn",
                        EntityId:         orderReturn.Id.Value),
                    ct);

                if (notificationResult.IsFailure)
                {
                    failedCount++;
                    _logger.LogWarning(
                        "OrderReturnReminderJob: notification dispatch failed for OrderReturn {OrderReturnId}: {Error}",
                        orderReturn.Id.Value, notificationResult.Error.Message);
                    continue;
                }

                orderReturn.MarkReminderSent(now);
                dbContext.Update(orderReturn);
                await unitOfWork.SaveChangesAsync(ct);

                sentCount++;
            }
            catch (Exception ex)
            {
                failedCount++;
                _logger.LogError(ex,
                    "OrderReturnReminderJob: failed sending reminder for OrderReturn {OrderReturnId}",
                    orderReturn.Id.Value);
            }
        }

        stopwatch.Stop();
        var logging = _loggingOptions.CurrentValue.Jobs;

        if (candidates.Count > 0 || failedCount > 0)
        {
            var level = failedCount > 0 || stopwatch.ElapsedMilliseconds >= logging.SlowJobThresholdMs
                ? LogLevel.Warning
                : LogLevel.Information;

            _logger.Log(
                level,
                "OrderReturnReminderJob completed in {DurationMs}ms. Candidates={Candidates}, Sent={Sent}, Failed={Failed}",
                stopwatch.ElapsedMilliseconds, candidates.Count, sentCount, failedCount);
        }
        else if (logging.LogNoopRuns)
        {
            _logger.LogDebug(
                "OrderReturnReminderJob found no eligible returns in {DurationMs}ms.",
                stopwatch.ElapsedMilliseconds);
        }
    }
}
