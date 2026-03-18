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

internal sealed class UserLockedOutEventHandler
    : INotificationHandler<UserLockedOutEvent>
{
    private readonly ILogger<UserLockedOutEventHandler> _logger;
    private readonly IUserMailNotifier _mailNotifier;
    private readonly IDbContext _dbContext;
    private readonly ISender _sender;

    public UserLockedOutEventHandler(
        ILogger<UserLockedOutEventHandler> logger,
        IUserMailNotifier mailNotifier,
        IDbContext dbContext,
        ISender sender)
    {
        _logger = logger;
        _mailNotifier = mailNotifier;
        _dbContext = dbContext;
        _sender = sender;
    }

    public async Task Handle(UserLockedOutEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "User {UserId} locked out until {LockoutEnd} after {FailedAttempts} failed attempts",
            notification.UserId,
            notification.LockoutEnd,
            notification.AccessFailedCount);

        if (!Guid.TryParse(notification.UserId, out var userIdValue))
        {
            _logger.LogWarning(
                "Skipped lockout follow-up because user id {UserId} is invalid.",
                notification.UserId);
            return;
        }

        var userId = UserId.From(userIdValue);
        var user = await _dbContext.GetByIdAsync<User, UserId>(
            userId,
            cancellationToken: cancellationToken);

        _dbContext.Set<AuditLog>().Add(
            AuditLog.Create(
                actorUserId: userId,
                actorRole: "system",
                action: "UserLockedOut",
                entityType: "User",
                entityId: userIdValue,
                oldData: null,
                newData: JsonSerializer.Serialize(new
                {
                    lockoutEnd = notification.LockoutEnd,
                    failedAttempts = notification.AccessFailedCount
                }),
                ipAddress: null,
                nowUtc: notification.OccurredAt));

        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: userIdValue,
                NotificationType: "security",
                EventType: "user_locked_out",
                Title: "Tai khoan tam thoi bi khoa",
                Message: "Tai khoan cua ban tam thoi bi khoa do qua nhieu lan dang nhap that bai.",
                Priority: NotificationPriority.Urgent,
                EntityType: "User",
                EntityId: userIdValue,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    userId = notification.UserId,
                    lockoutEnd = notification.LockoutEnd,
                    failedAttempts = notification.AccessFailedCount
                })),
            cancellationToken);

        if (user is not null)
        {
            await _mailNotifier.SendAccountLockedAlertAsync(
                user.Email.Value,
                user.UserName.Value,
                notification.LockoutEnd,
                notification.AccessFailedCount,
                cancellationToken);
        }
    }
}
