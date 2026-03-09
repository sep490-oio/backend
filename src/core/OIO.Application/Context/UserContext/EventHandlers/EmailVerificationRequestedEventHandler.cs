using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Mail;
using OIO.Application.Abstractions.Security;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.UserContext.EventHandlers;

internal sealed class EmailVerificationRequestedEventHandler
    : INotificationHandler<EmailVerificationRequestedEvent>
{
    private readonly ISecureTokenStore _secureTokenStore;
    private readonly IUserMailNotifier _mailNotifier;
    private readonly IAppConfigs _appConfigs;
    private readonly ILogger<EmailVerificationRequestedEventHandler> _logger;

    public EmailVerificationRequestedEventHandler(
        ISecureTokenStore secureTokenStore,
        IUserMailNotifier mailNotifier,
        IAppConfigs appConfigs,
        ILogger<EmailVerificationRequestedEventHandler> logger)
    {
        _secureTokenStore = secureTokenStore;
        _mailNotifier = mailNotifier;
        _appConfigs = appConfigs;
        _logger = logger;
    }

    public async Task Handle(
        EmailVerificationRequestedEvent notification,
        CancellationToken cancellationToken)
    {
        // Check cooldown
        var ttl = await _secureTokenStore.GetTokenTtlAsync(
            TokenType.EmailVerification, 
            UserId.Parse(notification.UserId),
            cancellationToken);

            
        var totalExpiration = await _appConfigs.Auth.GetEmailVerificationTokenExpirationMinutesAsync(cancellationToken);
        
        if (ttl.HasValue)
        {
            var elapsed = totalExpiration - ttl.Value;
            var cooldown = await _appConfigs.Auth.GetResendEmailCooldownSecondsAsync(cancellationToken);

            if (elapsed < cooldown)
            {
                _logger.LogWarning(
                    "Email verification cooldown for {UserId}. Wait {Remaining}s.",
                    notification.UserId, (cooldown - elapsed).TotalSeconds);
                return;
            }
        }

        // Create token
        var plainToken = await _secureTokenStore.CreateTokenAsync(
            TokenType.EmailVerification,
            UserId.Parse(notification.UserId),
            totalExpiration,
            cancellationToken);

        // Send email with userId + token
        await _mailNotifier.SendWelcomeVerifyAsync(
            toEmail: notification.Email,
            userName: notification.UserName,
            token: plainToken,
            userId: notification.UserId,
            cancellationToken);

        _logger.LogInformation(
            "Verification email sent to {Email} (UserId={UserId}).",
            notification.Email, notification.UserId);
    }
}

internal sealed class PasswordResetRequestedEventHandler : INotificationHandler<PasswordResetRequestedEvent>
{
    private readonly ISecureTokenStore _secureTokenStore;
    private readonly IUserMailNotifier _mailNotifier;
    private readonly IAppConfigs _appConfigs;
    private readonly ILogger<PasswordResetRequestedEventHandler> _logger;
    
    public PasswordResetRequestedEventHandler(
        ISecureTokenStore secureTokenStore,
        IUserMailNotifier mailNotifier,
        IAppConfigs appConfigs,
        ILogger<PasswordResetRequestedEventHandler> logger)
    {
        _secureTokenStore = secureTokenStore;
        _mailNotifier = mailNotifier;
        _appConfigs = appConfigs;
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
        
        var plainToken = await _secureTokenStore.CreateTokenAsync(
            TokenType.PasswordReset,
            UserId.Parse(notification.UserId),
            totalExpiration,
            cancellationToken: cancellationToken);
        
        await _mailNotifier.SendPasswordResetAsync(
            toEmail: notification.Email,
            userName: notification.UserName,
            token: plainToken,
            cancellationToken);

        _logger.LogInformation(
            "Password reset email sent to {Email} (UserId={UserId}).",
            notification.Email, notification.UserId);
    }
}