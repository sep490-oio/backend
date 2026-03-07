using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.UserContext.Aggregates.Users.Events;

public sealed record UserPhoneConfirmedEvent(
    string UserId,
    string PhoneNumber,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);