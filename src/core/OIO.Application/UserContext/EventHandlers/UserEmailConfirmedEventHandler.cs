using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;

namespace OIO.Application.UserContext.EventHandlers;

internal sealed class UserEmailConfirmedEventHandler
    : INotificationHandler<UserEmailConfirmedEvent>
{
    private readonly ILogger<UserEmailConfirmedEventHandler> _logger;

    public UserEmailConfirmedEventHandler(ILogger<UserEmailConfirmedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(UserEmailConfirmedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Email confirmed for user {UserId}: {Email}",
            notification.UserId,
            notification.Email);

        // TODO: Send confirmation success email
        // TODO: Notify notification context

        return Task.CompletedTask;
    }
}