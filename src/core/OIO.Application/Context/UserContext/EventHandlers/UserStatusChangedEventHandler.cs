using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Mail;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;

namespace OIO.Application.Context.UserContext.EventHandlers;

internal sealed class UserStatusChangedEventHandler
    : INotificationHandler<UserStatusChangedEvent>
{
    private readonly ILogger<UserStatusChangedEventHandler> _logger;
    private readonly IUserMailNotifier _mailNotifier;

    public UserStatusChangedEventHandler(
        ILogger<UserStatusChangedEventHandler> logger,
        IUserMailNotifier mailNotifier)
    {
        _logger = logger;
    }

    public Task Handle(UserStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "User {UserId} status changed from '{OldStatus}' to '{NewStatus}'",
            notification.UserId,
            notification.OldUserStatus,
            notification.NewUserStatus);

        // TODO: Notify user about account status change
        // TODO: If banned, trigger cleanup in other contexts

        return Task.CompletedTask;
    }
}