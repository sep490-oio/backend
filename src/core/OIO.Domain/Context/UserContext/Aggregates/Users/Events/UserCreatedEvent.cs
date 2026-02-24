using System.Net;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.UserContext.Aggregates.Users.Events;

public sealed record UserCreatedEvent(
    UserId UserId,
    UserName UserName,
    UserEmail Email,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record UserPasswordChangedEvent(
    UserId UserId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record UserEmailConfirmedEvent(
    UserId UserId,
    UserEmail Email,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record UserPhoneConfirmedEvent(
    UserId UserId,
    PhoneNumber PhoneNumber,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record UserStatusChangedEvent(
    UserId UserId,
    UserStatus OldUserStatus,
    UserStatus NewUserStatus,
    DateTime OccurredAt) 
    : DomainEvent(OccurredAt);

public sealed record UserLockedOutEvent(
    UserId UserId,
    DateTime LockoutEnd,
    int AccessFailedCount,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record LoginAttemptedEvent(
    UserId UserId,
    IPAddress IpAddress,
    string UserAgent,
    bool IsSuccess,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record SessionRevokedEvent(
    UserId UserId,
    UserRefreshTokenFamilyId SessionId,
    Guid DeviceId,
    string Reason,
    DateTime OccurredAt) 
    : DomainEvent(OccurredAt);

public sealed record RefreshTokenRotatedEvent(
    UserId UserId,
    UserRefreshTokenFamilyId SessionId,
    UserRefreshTokenId CurrentRefreshTokenId,
    UserRefreshTokenId NewRefreshTokenId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record SessionNearingExpirationEvent(
    UserId UserId,
    UserRefreshTokenFamilyId SessionId,
    DateTime AbsoluteExpiresAt,
    TimeSpan RemainingTime,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);
