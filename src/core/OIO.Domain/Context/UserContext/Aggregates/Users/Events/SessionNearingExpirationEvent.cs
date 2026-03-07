using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.UserContext.Aggregates.Users.Events;

public sealed record SessionNearingExpirationEvent(
    string UserId,
    string SessionId,
    DateTime AbsoluteExpiresAt,
    TimeSpan RemainingTime,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);
    
public sealed record EmailVerificationRequestedEvent(
    string UserId,
    string Email,
    string UserName,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);
    
public sealed record PasswordResetRequestedEvent(
    string UserId,
    string Email,
    string UserName,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);