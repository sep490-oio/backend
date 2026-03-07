using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Mail;
using OIO.Application.Abstractions.Security;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.UserContext.EventHandlers;

internal sealed class UserCreatedEventHandler
    : INotificationHandler<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedEventHandler> _logger;
    private readonly IUserMailNotifier _mailNotifier;
    private readonly ISecureTokenStore _secureTokenStore;

    public UserCreatedEventHandler(
        ILogger<UserCreatedEventHandler> logger,
        IUserMailNotifier mailNotifier,
        ISecureTokenStore secureTokenStore)
    {
        _logger = logger;
        _mailNotifier = mailNotifier;
        _secureTokenStore = secureTokenStore;
    }

    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "User created: {UserId}, UserName: {UserName}, Email: {Email}",
                notification.UserId,
                notification.UserName,
                notification.Email);

            var token = await _secureTokenStore.CreateTokenAsync(
                TokenType.EmailVerification,
                UserId.Parse(notification.UserId),
                cancellationToken: cancellationToken);

            await _mailNotifier.SendWelcomeVerifyAsync(
                toEmail: notification.Email,
                userName: notification.UserName,
                userId: notification.UserId,
                token: token, cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "An error occured during mail verification");
            throw;
        }
    }
}