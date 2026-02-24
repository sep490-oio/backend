using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;

namespace OIO.Application.UserContext.EventHandlers;

internal sealed class UserPasswordChangedEventHandler
    : INotificationHandler<UserPasswordChangedEvent>
{
    private readonly ILogger<UserPasswordChangedEventHandler> _logger;

    public UserPasswordChangedEventHandler(ILogger<UserPasswordChangedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(UserPasswordChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Password changed for user {UserId}",
            notification.UserId);

        // TODO: Send password changed security email

        return Task.CompletedTask;
    }
}