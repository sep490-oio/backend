using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;

namespace OIO.Application.UserContext.EventHandlers;

internal sealed class UserCreatedEventHandler
    : INotificationHandler<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedEventHandler> _logger;

    public UserCreatedEventHandler(ILogger<UserCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "User created: {UserId}, UserName: {UserName}, Email: {Email}",
            notification.UserId,
            notification.UserName,
            notification.Email);

        // TODO: Send welcome email
        // TODO: Publish integration event to message broker

        return Task.CompletedTask;
    }
}