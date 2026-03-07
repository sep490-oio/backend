namespace OIO.Application.Context.UserContext.DTOs;

public sealed record SessionExpirationDto(
    Guid SessionId,
    Guid DeviceId,
    DateTime SlidingExpiresAt,
    DateTime AbsoluteExpiresAt,
    bool IsNearingAbsoluteExpiration,
    TimeSpan RemainingAbsoluteTime);