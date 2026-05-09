using System.Diagnostics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.NotificationContext.Enums;
using Quartz;

namespace OIO.Infrastructure.Scheduling.Jobs.Disputes;

/// <summary>
/// Hourly sweep that auto-escalates <see cref="Dispute"/> rows stuck in
/// <see cref="DisputeStatus.AwaitingRespondent"/> past their
/// <c>ResponseDeadline</c>. Transitions the dispute to
/// <see cref="DisputeStatus.UnderReview"/> and notifies both parties.
/// Default deadline: 3 business days from transition to awaiting_respondent.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class DisputeResponseDeadlineWatcherJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClock _clock;
    private readonly IOptionsMonitor<AppLoggingOptions> _loggingOptions;
    private readonly ILogger<DisputeResponseDeadlineWatcherJob> _logger;

    public DisputeResponseDeadlineWatcherJob(
        IServiceScopeFactory scopeFactory,
        IClock clock,
        IOptionsMonitor<AppLoggingOptions> loggingOptions,
        ILogger<DisputeResponseDeadlineWatcherJob> logger)
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
        var dbContext   = scope.ServiceProvider.GetRequiredService<IDbContext>();
        var unitOfWork  = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var sender      = scope.ServiceProvider.GetRequiredService<ISender>();

        var ct  = context.CancellationToken;
        var now = _clock.UtcNow;

        // Find disputes in awaiting_respondent with an elapsed deadline.
        var overdue = await dbContext.Set<Dispute>()
            .Where(d => d.Status == DisputeStatus.AwaitingRespondent
                     && d.ResponseDeadline != null
                     && d.ResponseDeadline < now)
            .Include(d => d.StatusHistory)
            .OrderBy(d => d.ResponseDeadline)
            .Take(100)
            .ToListAsync(ct);

        var escalatedCount = 0;
        var failedCount    = 0;

        foreach (var dispute in overdue)
        {
            try
            {
                // Auto-transition to under_review so admin picks it up.
                var result = dispute.TransitionTo(DisputeStatus.UnderReview, now);
                if (result.IsFailure)
                {
                    failedCount++;
                    _logger.LogWarning(
                        "DisputeResponseDeadlineWatcher: TransitionTo failed for Dispute {DisputeId}: {Error}",
                        dispute.Id.Value, result.Error.Message);
                    continue;
                }

                dbContext.Update(dispute);
                await unitOfWork.SaveChangesAsync(ct);
                escalatedCount++;

                // Best-effort notifications — do not fail the job on notification errors.
                try
                {
                    // Notify complainant
                    await sender.Send(
                        new CreateNotificationCommand(
                            UserId: dispute.ComplainantId.Value,
                            NotificationType: "dispute",
                            EventType: "dispute_escalated",
                            Title: "Khieu nai cua ban da duoc chuyen len",
                            Message: $"Khieu nai #{dispute.DisputeNumber.Value} da duoc chuyen len do ben lien quan khong phan hoi trong thoi han.",
                            Priority: NotificationPriority.High,
                            EntityType: "Dispute",
                            EntityId: dispute.Id.Value),
                        ct);

                    // Notify respondent (if exists)
                    if (dispute.RespondentId.Value != Guid.Empty)
                    {
                        await sender.Send(
                            new CreateNotificationCommand(
                                UserId: dispute.RespondentId.Value,
                                NotificationType: "dispute",
                                EventType: "dispute_escalated",
                                Title: "Khieu nai da het han phan hoi",
                                Message: $"Ban da khong phan hoi khieu nai #{dispute.DisputeNumber.Value} trong thoi han. Vu viec da duoc chuyen len admin.",
                                Priority: NotificationPriority.High,
                                EntityType: "Dispute",
                                EntityId: dispute.Id.Value),
                            ct);
                    }
                }
                catch (Exception notifEx)
                {
                    _logger.LogWarning(notifEx,
                        "DisputeResponseDeadlineWatcher: notification dispatch failed for Dispute {DisputeId}. Escalation committed; notifications skipped.",
                        dispute.Id.Value);
                }
            }
            catch (Exception ex)
            {
                failedCount++;
                _logger.LogError(ex,
                    "DisputeResponseDeadlineWatcher: error escalating Dispute {DisputeId}",
                    dispute.Id.Value);
            }
        }

        stopwatch.Stop();
        var logging = _loggingOptions.CurrentValue.Jobs;

        if (overdue.Count > 0 || failedCount > 0)
        {
            var level = failedCount > 0 || stopwatch.ElapsedMilliseconds >= logging.SlowJobThresholdMs
                ? LogLevel.Warning
                : LogLevel.Information;

            _logger.Log(
                level,
                "DisputeResponseDeadlineWatcherJob completed in {DurationMs}ms. Eligible={Eligible}, Escalated={Escalated}, Failed={Failed}",
                stopwatch.ElapsedMilliseconds, overdue.Count, escalatedCount, failedCount);
        }
        else if (logging.LogNoopRuns)
        {
            _logger.LogDebug(
                "DisputeResponseDeadlineWatcherJob found no overdue disputes in {DurationMs}ms.",
                stopwatch.ElapsedMilliseconds);
        }
    }
}
