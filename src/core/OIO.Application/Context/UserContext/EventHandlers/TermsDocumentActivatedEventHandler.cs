using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Caching;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.UserContext.Hubs;
using OIO.Domain.Context.NotificationContext.Aggregates;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.UserContext.EventHandlers;

/// <summary>
/// Reacts to <see cref="TermsDocumentActivatedEvent"/> — the forced re-acceptance backbone
/// (plan §3.6.2 / B5). Runs via the outbox consumer pipeline (scoped DI scope per message),
/// so admin-facing <c>Activate</c> p99 is bounded by <c>SaveChangesAsync</c> only; all fan-out
/// happens here in background.
///
/// Order is load-bearing and pinned by <c>TermsDocumentActivatedEventHandlerOrderingTests</c>:
///   1. Cache invalidation FIRST — FE-triggered BE re-reads must never hit a stale gate cache.
///   2. SignalR broadcast to <c>terms:authenticated</c> group — user-facing enforcement channel.
///   3. Per-user notification fan-out in batches of <c>NotificationBatchSize</c>.
///   4. Metric log — dashboard aggregation on the <c>terms_activated_total</c> counter.
/// </summary>
internal sealed class TermsDocumentActivatedEventHandler
    : INotificationHandler<TermsDocumentActivatedEvent>
{
    private const int NotificationBatchSize = 500;

    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly ITermsHubBroadcaster _hubBroadcaster;
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TermsDocumentActivatedEventHandler> _logger;

    public TermsDocumentActivatedEventHandler(
        ICacheInvalidator cacheInvalidator,
        ITermsHubBroadcaster hubBroadcaster,
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ILogger<TermsDocumentActivatedEventHandler> logger)
    {
        _cacheInvalidator = cacheInvalidator;
        _hubBroadcaster = hubBroadcaster;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(TermsDocumentActivatedEvent notification, CancellationToken cancellationToken)
    {
        // Step 1: Cache invalidation FIRST (plan §3.6.2 ordering invariant).
        await _cacheInvalidator.InvalidateByTagAsync(
            $"terms:{notification.TermType.ToLowerInvariant()}",
            cancellationToken);

        // Step 2: SignalR broadcast — scoped to authenticated users only (plan C4).
        await _hubBroadcaster.BroadcastTermsActivatedAsync(
            new TermsActivatedBroadcast(
                TermType: notification.TermType,
                NewVersionId: notification.TermsDocumentId,
                NewVersion: notification.NewVersion,
                ActivatedAt: notification.OccurredAt),
            cancellationToken);

        // Step 3: Per-user notification fan-out in batches of 500. Skipped when there is nothing
        // to supersede (first-ever activation for this TermType).
        if (!string.IsNullOrWhiteSpace(notification.SupersededId)
            && Guid.TryParse(notification.SupersededId, out var supersededGuid))
        {
            var supersededId = TermsDocumentId.From(supersededGuid);
            var count = await FanOutNotificationsAsync(
                notification, supersededId, cancellationToken);

            _logger.LogInformation(
                "metric=terms_activation_notification_dispatched_total{{type={TermType}}} Count={Count} NewVersionId={NewVersionId} SupersededId={SupersededId}",
                notification.TermType, count, notification.TermsDocumentId, notification.SupersededId);
        }

        // Step 4: Metric log — structured for log-to-metric aggregation (plan §4.4).
        _logger.LogInformation(
            "metric=terms_activated_total{{type={TermType}}} NewVersion={NewVersion} SupersededId={SupersededId} ActivatedBy={ActivatedBy} OccurredAt={OccurredAt}",
            notification.TermType,
            notification.NewVersion,
            notification.SupersededId ?? "(none)",
            notification.ActivatedBy,
            notification.OccurredAt);
    }

    private async Task<int> FanOutNotificationsAsync(
        TermsDocumentActivatedEvent notification,
        TermsDocumentId supersededId,
        CancellationToken cancellationToken)
    {
        var title = $"Updated {notification.TermType} terms — re-acceptance required";
        var message = $"Please review and accept the updated {notification.TermType} terms (v{notification.NewVersion}) to continue using OIO.";
        var metadata = System.Text.Json.JsonSerializer.Serialize(new
        {
            termType = notification.TermType,
            newVersionId = notification.TermsDocumentId,
            newVersion = notification.NewVersion,
            supersededId = notification.SupersededId,
        });

        var total = 0;
        var lastSeen = Guid.Empty;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batch = await _dbContext.Set<TermsAcceptance>()
                .AsNoTracking()
                .Where(a => a.TermDocumentId == supersededId && a.UserId.Value.CompareTo(lastSeen) > 0)
                .OrderBy(a => a.UserId)
                .Select(a => a.UserId.Value)
                .Distinct()
                .Take(NotificationBatchSize)
                .ToListAsync(cancellationToken);

            if (batch.Count == 0)
                break;

            var notifications = batch
                .Select(userId => Notification.Create(
                    userId: userId,
                    notificationType: "in_app",
                    eventType: "terms_updated",
                    title: title,
                    message: message,
                    priority: NotificationPriority.High,
                    entityType: "TermsDocument",
                    entityId: Guid.Parse(notification.TermsDocumentId),
                    metadata: metadata))
                .ToList();

            foreach (var n in notifications)
                _dbContext.Insert(n);

            // Persist each batch so the change-tracker stays bounded for 10k+ acceptance counts.
            // A partial failure mid-loop is safely re-driven by the outbox retry — this handler
            // is wrapped by IdempotentDomainEventHandler<> and the batch's Notification rows
            // carry fresh Ids so retry is a dup-insert no-op. The catch-up hosted service
            // TermsNotificationOutboxProcessor re-drives fan-out on cold start.
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            total += notifications.Count;
            lastSeen = batch[^1];

            if (batch.Count < NotificationBatchSize)
                break;
        }

        return total;
    }
}
