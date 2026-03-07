using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.UserContext.Aggregates.Users.Events;

public sealed record SessionRevokedEvent(
    string UserId,
    string SessionId,
    Guid DeviceId,
    string Reason,
    DateTime OccurredAt) 
    : DomainEvent(OccurredAt);