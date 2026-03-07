using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.UserContext.Aggregates.Users.Events;

public sealed record LoginAttemptedEvent(
    string UserId,
    string IpAddress,
    string UserAgent,
    bool IsSuccess,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);