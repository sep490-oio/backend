using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.UserContext.Aggregates.Users.Events;

public sealed record UserLockedOutEvent(
    string UserId,
    DateTime LockoutEnd,
    int AccessFailedCount,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);