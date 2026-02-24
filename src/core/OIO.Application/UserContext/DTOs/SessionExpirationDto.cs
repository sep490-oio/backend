namespace OIO.Application.UserContext.DTOs;

public sealed record SessionExpirationDto(
    Guid SessionId,
    Guid DeviceId,
    DateTime SlidingExpiresAt,
    DateTime AbsoluteExpiresAt,
    bool IsNearingAbsoluteExpiration,
    TimeSpan RemainingAbsoluteTime);