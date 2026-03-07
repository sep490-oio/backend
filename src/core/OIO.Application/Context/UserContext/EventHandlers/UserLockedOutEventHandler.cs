using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Mail;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;

namespace OIO.Application.Context.UserContext.EventHandlers;

internal sealed class UserLockedOutEventHandler
    : INotificationHandler<UserLockedOutEvent>
{
    private readonly ILogger<UserLockedOutEventHandler> _logger;
    private readonly IUserMailNotifier _mailNotifier;

    public UserLockedOutEventHandler(
        ILogger<UserLockedOutEventHandler> logger,
        IUserMailNotifier mailNotifier)
    {
        _logger = logger;
        _mailNotifier = mailNotifier;
    }

    public Task Handle(UserLockedOutEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "User {UserId} locked out until {LockoutEnd} after {FailedAttempts} failed attempts",
            notification.UserId,
            notification.LockoutEnd,
            notification.AccessFailedCount);

        // TODO: Send security alert email
        // TODO: Log to security audit system

        return Task.CompletedTask;
    }
}