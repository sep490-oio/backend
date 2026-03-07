using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.UserContext.Aggregates.Users.Events;

public sealed record UserStatusChangedEvent(
    string UserId,
    string OldUserStatus,
    string NewUserStatus,
    DateTime OccurredAt) 
    : DomainEvent(OccurredAt);