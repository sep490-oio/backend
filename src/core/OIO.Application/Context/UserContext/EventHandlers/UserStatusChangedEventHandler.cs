using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Mail;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.UserContext.EventHandlers;

internal sealed class UserStatusChangedEventHandler
    : INotificationHandler<UserStatusChangedEvent>
{
    private readonly ILogger<UserStatusChangedEventHandler> _logger;
    private readonly IUserMailNotifier _mailNotifier;
    private readonly IDbContext _dbContext;
    private readonly ISender _sender;
    private readonly ISessionRevocationStore _sessionRevocationStore;

    public UserStatusChangedEventHandler(
        ILogger<UserStatusChangedEventHandler> logger,
        IUserMailNotifier mailNotifier,
        IDbContext dbContext,
        ISender sender,
        ISessionRevocationStore sessionRevocationStore)
    {
        _logger = logger;
        _mailNotifier = mailNotifier;
        _dbContext = dbContext;
        _sender = sender;
        _sessionRevocationStore = sessionRevocationStore;
    }

    public async Task Handle(UserStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "User {UserId} status changed from '{OldStatus}' to '{NewStatus}'",
            notification.UserId,
            notification.OldUserStatus,
            notification.NewUserStatus);

        if (!Guid.TryParse(notification.UserId, out var userIdValue))
        {
            _logger.LogWarning(
                "Skipped status changed follow-up because user id {UserId} is invalid.",
                notification.UserId);
            return;
        }

        var userId = UserId.From(userIdValue);
        var user = await _dbContext.GetByIdAsync<User, UserId>(
            userId,
            cancellationToken: cancellationToken);

        var isRevokedStatus = UserStatus.RevokedStatus.Any(
            x => string.Equals(x.Id, notification.NewUserStatus, StringComparison.OrdinalIgnoreCase));

        _dbContext.Set<AuditLog>().Add(
            AuditLog.Create(
                actorUserId: userId,
                actorRole: "system",
                action: "UserStatusChanged",
                entityType: "User",
                entityId: userIdValue,
                oldData: JsonSerializer.Serialize(new { status = notification.OldUserStatus }),
                newData: JsonSerializer.Serialize(new { status = notification.NewUserStatus }),
                ipAddress: null,
                nowUtc: notification.OccurredAt));

        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: userIdValue,
                NotificationType: "account",
                EventType: "user_status_changed",
                Title: $"Trang thai tai khoan da doi sang {notification.NewUserStatus}",
                Message: $"Trang thai tai khoan cua ban da duoc doi tu {notification.OldUserStatus} sang {notification.NewUserStatus}.",
                Priority: isRevokedStatus ? NotificationPriority.High : NotificationPriority.Normal,
                EntityType: "User",
                EntityId: userIdValue,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    userId = notification.UserId,
                    oldStatus = notification.OldUserStatus,
                    newStatus = notification.NewUserStatus
                })),
            cancellationToken);

        if (user is not null)
        {
            await _mailNotifier.SendAccountStatusChangedAsync(
                user.Email.Value,
                user.UserName.Value,
                notification.OldUserStatus,
                notification.NewUserStatus,
                cancellationToken);
        }

        if (isRevokedStatus)
        {
            await _sessionRevocationStore.RevokeAllDevicesAsync(userId, cancellationToken);
        }
    }
}
