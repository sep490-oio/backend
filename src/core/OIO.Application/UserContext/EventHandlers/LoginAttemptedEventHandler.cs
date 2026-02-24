using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;

namespace OIO.Application.UserContext.EventHandlers;

internal sealed class LoginAttemptedEventHandler
    : INotificationHandler<LoginAttemptedEvent>
{
    private readonly ILogger<LoginAttemptedEventHandler> _logger;

    public LoginAttemptedEventHandler(ILogger<LoginAttemptedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(LoginAttemptedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.IsSuccess)
        {
            _logger.LogInformation(
                "Successful login for user {UserId} from {IpAddress}",
                notification.UserId,
                notification.IpAddress);
        }
        else
        {
            _logger.LogWarning(
                "Failed login attempt for user {UserId} from {IpAddress}",
                notification.UserId,
                notification.IpAddress);
        }

        // TODO: Detect suspicious login patterns (new location, etc.)
        // TODO: Rate limiting on failed attempts from same IP

        return Task.CompletedTask;
    }
}