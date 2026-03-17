using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
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
    private readonly IRuntimeSettings _runtimeSettings;
    private readonly IClock _clock;
    private readonly ILogger<EmailVerificationRequestedEventHandler> _logger;

    public EmailVerificationRequestedEventHandler(
        ISecureTokenStore secureTokenStore,
        IUserMailNotifier mailNotifier,
        IRuntimeSettings runtimeSettings,
        IClock clock,
        ILogger<EmailVerificationRequestedEventHandler> logger)
    {
        _secureTokenStore = secureTokenStore;
        _mailNotifier = mailNotifier;
        _runtimeSettings = runtimeSettings;
        _clock = clock;
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

            
        var totalExpiration = _runtimeSettings.Auth.EmailVerificationTokenExpiration;
        
        if (ttl.HasValue)
        {
            var elapsed = totalExpiration - ttl.Value;
            var cooldown = _runtimeSettings.Auth.ResendEmailCooldown;

            if (elapsed < cooldown)
            {
                _logger.LogWarning(
                    "Email verification cooldown for {UserId}. Wait {Remaining}s.",
                    notification.UserId, (cooldown - elapsed).TotalSeconds);
                return;
            }
        }

        // Create token
        (var plainToken, ttl) = await _secureTokenStore.CreateTokenAsync(
            TokenType.EmailVerification,
            UserId.Parse(notification.UserId),
            totalExpiration,
            cancellationToken);

        // Send email with userId + token
        await _mailNotifier.SendResendVerifyAsync(
            toEmail: notification.Email,
            userName: notification.UserName,
            token: plainToken,
            userId: notification.UserId,
            tokenExpiry: _clock.UtcNow.Add(ttl.Value),
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Verification email sent to {Email} (UserId={UserId}).",
            notification.Email, notification.UserId);
    }
}
