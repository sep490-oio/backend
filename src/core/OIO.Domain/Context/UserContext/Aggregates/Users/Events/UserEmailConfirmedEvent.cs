using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.UserContext.Aggregates.Users.Events;

public sealed record UserEmailConfirmedEvent(
    string UserId,
    string Email,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);