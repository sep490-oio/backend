using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Mail;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.UserContext.EventHandlers;

internal sealed class SessionRevokedEventHandler
    : INotificationHandler<SessionRevokedEvent>
{
    private readonly ILogger<SessionRevokedEventHandler> _logger;
    private readonly IDbContext _dbContext;
    private readonly IUserMailNotifier _mailNotifier;
    private readonly ISender _sender;

    public SessionRevokedEventHandler(
        ILogger<SessionRevokedEventHandler> logger,
        IDbContext dbContext,
        IUserMailNotifier mailNotifier,
        ISender sender)
    {
        _logger = logger;
        _dbContext = dbContext;
        _mailNotifier = mailNotifier;
        _sender = sender;
    }

    public async Task Handle(SessionRevokedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Token family {FamilyId} revoked for user {UserId}. Reason: {Reason}",
            notification.SessionId,
            notification.UserId,
            notification.Reason);

        if (!Guid.TryParse(notification.UserId, out var userIdValue))
        {
            _logger.LogWarning(
                "Skipped revoked session follow-up because user id {UserId} is invalid.",
                notification.UserId);
            return;
        }

        var userId = UserId.From(userIdValue);
        var user = await _dbContext.GetByIdAsync<User, UserId>(
            userId,
            cancellationToken: cancellationToken);

        Guid? sessionEntityId = Guid.TryParse(notification.SessionId, out var parsedSessionId)
            ? parsedSessionId
            : null;

        var isSecuritySensitive = IsSecuritySensitiveReason(notification.Reason);

        _dbContext.Set<AuditLog>().Add(
            AuditLog.Create(
                actorUserId: userId,
                actorRole: "system",
                action: "SessionRevoked",
                entityType: "UserSession",
                entityId: sessionEntityId,
                oldData: null,
                newData: JsonSerializer.Serialize(new
                {
                    sessionId = notification.SessionId,
                    deviceId = notification.DeviceId,
                    reason = notification.Reason,
                    securitySensitive = isSecuritySensitive
                }),
                ipAddress: null,
                nowUtc: notification.OccurredAt));

        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: userIdValue,
                NotificationType: isSecuritySensitive ? "security" : "session",
                EventType: "session_revoked",
                Title: isSecuritySensitive ? "Canh bao bao mat cho phien dang nhap" : "Phien dang nhap da bi thu hoi",
                Message: isSecuritySensitive
                    ? "Mot phien dang nhap cua ban da bi thu hoi vi dau hieu bat thuong. Hay doi mat khau neu day khong phai ban."
                    : "Mot phien dang nhap cua ban da bi thu hoi.",
                Priority: isSecuritySensitive ? NotificationPriority.High : NotificationPriority.Normal,
                EntityType: "Session",
                EntityId: sessionEntityId,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    sessionId = notification.SessionId,
                    deviceId = notification.DeviceId,
                    reason = notification.Reason
                })),
            cancellationToken);

        if (isSecuritySensitive && user is not null)
        {
            await _mailNotifier.SendSecuritySessionRevokedAlertAsync(
                user.Email.Value,
                user.UserName.Value,
                notification.Reason,
                notification.DeviceId,
                cancellationToken);
        }
    }

    private static bool IsSecuritySensitiveReason(string reason)
    {
        return reason.Contains("reuse", StringComparison.OrdinalIgnoreCase) ||
               reason.Contains("mismatch", StringComparison.OrdinalIgnoreCase) ||
               reason.Contains("theft", StringComparison.OrdinalIgnoreCase);
    }
}
