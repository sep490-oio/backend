using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;

namespace OIO.Application.UserContext.EventHandlers;

internal sealed class TokenFamilyRevokedEventHandler
    : INotificationHandler<SessionRevokedEvent>
{
    private readonly ILogger<TokenFamilyRevokedEventHandler> _logger;

    public TokenFamilyRevokedEventHandler(ILogger<TokenFamilyRevokedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(SessionRevokedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Token family {FamilyId} revoked for user {UserId}. Reason: {Reason}",
            notification.SessionId,
            notification.UserId,
            notification.Reason);

        // TODO: If reason is token reuse, send security alert
        // TODO: Log to security audit

        return Task.CompletedTask;
    }
}