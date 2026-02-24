using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;

namespace OIO.Application.UserContext.EventHandlers;

internal sealed class SessionNearingExpirationEventHandler
    : INotificationHandler<SessionNearingExpirationEvent>
{
    private readonly ILogger<SessionNearingExpirationEventHandler> _logger;

    public SessionNearingExpirationEventHandler(
        ILogger<SessionNearingExpirationEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(
        SessionNearingExpirationEvent notification,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Session nearing absolute expiration for user {UserId}. " +
            "Session: {SessionId}, Expires at: {AbsoluteExpiresAt}, " +
            "Remaining: {RemainingTime}",
            notification.UserId,
            notification.SessionId,
            notification.AbsoluteExpiresAt,
            notification.RemainingTime);

        // TODO: Send push notification to user
        //   "Your session will expire in X hours. Please save your work."
        // TODO: Publish integration event to Notification Context

        return Task.CompletedTask;
    }
}