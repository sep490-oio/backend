using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;

namespace OIO.Application.Context.UserContext.EventHandlers;

internal sealed class SessionRevokedEventHandler
    : INotificationHandler<SessionRevokedEvent>
{
    private readonly ILogger<SessionRevokedEventHandler> _logger;

    public SessionRevokedEventHandler(ILogger<SessionRevokedEventHandler> logger)
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