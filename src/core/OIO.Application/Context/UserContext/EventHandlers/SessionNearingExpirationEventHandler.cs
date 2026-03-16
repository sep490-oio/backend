using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;

namespace OIO.Application.Context.UserContext.EventHandlers;

internal sealed class SessionNearingExpirationEventHandler
    : INotificationHandler<SessionNearingExpirationEvent>
{
    private readonly ILogger<SessionNearingExpirationEventHandler> _logger;
    private readonly ISender _sender;

    public SessionNearingExpirationEventHandler(
        ILogger<SessionNearingExpirationEventHandler> logger,
        ISender sender)
    {
        _logger = logger;
        _sender = sender;
    }

    public async Task Handle(
        SessionNearingExpirationEvent notification,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Session nearing absolute expiration for user {UserId}. " +
            "Session: {SessionId}, Expires at: {AbsoluteExpiresAt}, " +
            "Remaining: {RemainingTime}",
            notification.UserId,
            notification.SessionId,
            notification.AbsoluteExpiresAt,
            notification.RemainingTime);

        if (!Guid.TryParse(notification.UserId, out var userId))
        {
            _logger.LogWarning(
                "Skipped session nearing expiration notification because user id {UserId} is invalid.",
                notification.UserId);
            return;
        }

        Guid? sessionId = Guid.TryParse(notification.SessionId, out var parsedSessionId)
            ? parsedSessionId
            : null;

        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: userId,
                NotificationType: "session",
                EventType: "session_nearing_expiration",
                Title: "Phien dang nhap sap het han",
                Message: $"Phien dang nhap cua ban se het han sau {FormatRemainingTime(notification.RemainingTime)}. Hay luu cong viec va dang nhap lai neu can.",
                Priority: NotificationPriority.Normal,
                EntityType: "Session",
                EntityId: sessionId,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    sessionId = notification.SessionId,
                    absoluteExpiresAt = notification.AbsoluteExpiresAt,
                    remainingSeconds = (int)Math.Max(0, notification.RemainingTime.TotalSeconds)
                })),
            cancellationToken);
    }

    private static string FormatRemainingTime(TimeSpan remainingTime)
    {
        if (remainingTime.TotalHours >= 1)
        {
            return $"{Math.Ceiling(remainingTime.TotalHours)} gio";
        }

        if (remainingTime.TotalMinutes >= 1)
        {
            return $"{Math.Ceiling(remainingTime.TotalMinutes)} phut";
        }

        return $"{Math.Max(1, (int)Math.Ceiling(remainingTime.TotalSeconds))} giay";
    }
}
