using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Mail;
using OIO.Application.Abstractions.Security;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.UserContext.EventHandlers;

internal sealed class PasswordResetRequestedEventHandler : INotificationHandler<PasswordResetRequestedEvent>
{
    private readonly ISecureTokenStore _secureTokenStore;
    private readonly IUserMailNotifier _mailNotifier;
    private readonly IAppConfigs _appConfigs;
    private readonly IClock _clock;
    private readonly ILogger<PasswordResetRequestedEventHandler> _logger;
    
    public PasswordResetRequestedEventHandler(
        ISecureTokenStore secureTokenStore,
        IUserMailNotifier mailNotifier,
        IAppConfigs appConfigs,
        IClock clock,
        ILogger<PasswordResetRequestedEventHandler> logger)
    {
        _secureTokenStore = secureTokenStore;
        _mailNotifier = mailNotifier;
        _appConfigs = appConfigs;
        _clock = clock;
        _logger = logger;
    }
    
    public async Task Handle(PasswordResetRequestedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing password reset for {Email} (UserId={UserId})",
            notification.Email, notification.UserId);
        
        var ttl = await _secureTokenStore.GetTokenTtlAsync(
            TokenType.PasswordReset, 
            UserId.Parse(notification.UserId),
            cancellationToken);

        var totalExpiration = await _appConfigs.Auth.GetPasswordResetTokenExpirationMinutesAsync(cancellationToken);
        
        if (ttl.HasValue)
        {
            var elapsed = totalExpiration - ttl.Value;
            var cooldown = await _appConfigs.Auth.GetResendEmailCooldownSecondsAsync(cancellationToken);

            if (elapsed < cooldown)
            {
                _logger.LogWarning(
                    "Password reset cooldown for {UserId}. Wait {Remaining}s.",
                    notification.UserId, (cooldown - elapsed).TotalSeconds);
                return;
            }
        }
        
        (var plainToken, ttl) = await _secureTokenStore.CreateTokenAsync(
            TokenType.PasswordReset,
            UserId.Parse(notification.UserId),
            totalExpiration,
            cancellationToken: cancellationToken);

        await _mailNotifier.SendPasswordResetAsync(
            toEmail: notification.Email,
            userName: notification.UserName,
            token: plainToken,
            tokenExpiry: _clock.UtcNow.Add(ttl.Value),
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Password reset email sent to {Email} (UserId={UserId}).",
            notification.Email, notification.UserId);
    }
}