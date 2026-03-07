using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.UserContext.Aggregates.Users.Events;

public sealed record UserPasswordChangedEvent(
    string UserId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);