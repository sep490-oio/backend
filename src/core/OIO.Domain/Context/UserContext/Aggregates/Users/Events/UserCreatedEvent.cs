using System.Net;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.UserContext.Aggregates.Users.Events;

public sealed record UserCreatedEvent(
    string UserId,
    string UserName,
    string Email,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record UserPasswordChangedEvent(
    string UserId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record UserEmailConfirmedEvent(
    string UserId,
    string Email,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record UserPhoneConfirmedEvent(
    string UserId,
    string PhoneNumber,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record UserStatusChangedEvent(
    string UserId,
    string OldUserStatus,
    string NewUserStatus,
    DateTime OccurredAt) 
    : DomainEvent(OccurredAt);

public sealed record UserLockedOutEvent(
    string UserId,
    DateTime LockoutEnd,
    int AccessFailedCount,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record LoginAttemptedEvent(
    string UserId,
    string IpAddress,
    string UserAgent,
    bool IsSuccess,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record SessionRevokedEvent(
    string UserId,
    string SessionId,
    Guid DeviceId,
    string Reason,
    DateTime OccurredAt) 
    : DomainEvent(OccurredAt);

public sealed record RefreshTokenRotatedEvent(
    string UserId,
    string SessionId,
    string CurrentRefreshTokenId,
    string NewRefreshTokenId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record SessionNearingExpirationEvent(
    string UserId,
    string SessionId,
    DateTime AbsoluteExpiresAt,
    TimeSpan RemainingTime,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);
