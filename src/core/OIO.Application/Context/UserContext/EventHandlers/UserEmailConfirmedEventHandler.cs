using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Mail;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;

namespace OIO.Application.Context.UserContext.EventHandlers;

internal sealed class UserEmailConfirmedEventHandler
    : INotificationHandler<UserEmailConfirmedEvent>
{
    private readonly ILogger<UserEmailConfirmedEventHandler> _logger;
    private readonly IUserMailNotifier _mailNotifier;

    public UserEmailConfirmedEventHandler(
        ILogger<UserEmailConfirmedEventHandler> logger,
        IUserMailNotifier mailNotifier)
    {
        _logger = logger;
        _mailNotifier = mailNotifier;
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