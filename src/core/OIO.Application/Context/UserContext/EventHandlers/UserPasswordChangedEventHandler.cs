using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Mail;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.UserContext.EventHandlers;

internal sealed class UserPasswordChangedEventHandler
    : INotificationHandler<UserPasswordChangedEvent>
{
    private readonly IDbContext _dbContext;
    private readonly IUserMailNotifier _mailNotifier;
    private readonly ISessionRevocationStore _sessionRevocationStore;
    private readonly ILogger<UserPasswordChangedEventHandler> _logger;

    public UserPasswordChangedEventHandler(
        IDbContext dbContext,
        IUserMailNotifier mailNotifier,
        ISessionRevocationStore sessionRevocationStore,
        ILogger<UserPasswordChangedEventHandler> logger)
    {
        _dbContext = dbContext;
        _mailNotifier = mailNotifier;
        _sessionRevocationStore = sessionRevocationStore;
        _logger = logger;
    }

    public async Task Handle(UserPasswordChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Password changed for user {UserId}",
            notification.UserId);
        
        var userId = UserId.Parse(notification.UserId);

        var user = await _dbContext.GetByIdAsync<User, UserId>(
            id: userId,
            cancellationToken: cancellationToken
        );
        
        if (user is null)
        {
            _logger.LogWarning(
                "User {UserId} not found for password changed alert.",
                notification.UserId);
            return;
        }

        await _mailNotifier.SendPasswordChangedAlertAsync(
            user.Email.Value,
            user.UserName.Value,
            cancellationToken);
        
        _logger.LogInformation(
                "Password changed alert sent to {Email}.", user.Email.Value);
        
        await _sessionRevocationStore.RevokeAllDevicesAsync(userId, cancellationToken);
    }
}