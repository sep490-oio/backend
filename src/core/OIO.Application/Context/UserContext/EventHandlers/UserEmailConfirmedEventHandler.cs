using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Mail;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.UserContext.EventHandlers;

internal sealed class UserEmailConfirmedEventHandler
    : INotificationHandler<UserEmailConfirmedEvent>
{
    private readonly ILogger<UserEmailConfirmedEventHandler> _logger;
    private readonly IUserMailNotifier _mailNotifier;
    private readonly IDbContext _dbContext;
    private readonly ISender _sender;

    public UserEmailConfirmedEventHandler(
        ILogger<UserEmailConfirmedEventHandler> logger,
        IUserMailNotifier mailNotifier,
        IDbContext dbContext,
        ISender sender)
    {
        _logger = logger;
        _mailNotifier = mailNotifier;
        _dbContext = dbContext;
        _sender = sender;
    }

    public async Task Handle(UserEmailConfirmedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Email confirmed for user {UserId}: {Email}",
            notification.UserId,
            notification.Email);

        if (!Guid.TryParse(notification.UserId, out var userIdValue))
        {
            _logger.LogWarning(
                "Skipped email confirmed follow-up because user id {UserId} is invalid.",
                notification.UserId);
            return;
        }

        var user = await _dbContext.GetByIdAsync<User, UserId>(
            UserId.From(userIdValue),
            cancellationToken: cancellationToken);

        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: userIdValue,
                NotificationType: "account",
                EventType: "user_email_confirmed",
                Title: "Email da duoc xac thuc",
                Message: "Email cua ban da duoc xac thuc thanh cong.",
                Priority: NotificationPriority.Normal,
                EntityType: "User",
                EntityId: userIdValue,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    userId = notification.UserId,
                    email = notification.Email,
                    confirmedAt = notification.OccurredAt
                })),
            cancellationToken);

        if (user is not null)
        {
            await _mailNotifier.SendEmailConfirmedAsync(
                notification.Email,
                user.UserName.Value,
                cancellationToken);
        }
    }
}
