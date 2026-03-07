using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.UserContext.Aggregates.Users.Events;

public sealed record RefreshTokenRotatedEvent(
    string UserId,
    string SessionId,
    string CurrentRefreshTokenId,
    string NewRefreshTokenId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);