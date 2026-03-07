namespace OIO.Application.Context.UserContext.DTOs;

public sealed record UserSessionDto(
    Guid SessionId,
    Guid DeviceId,
    string UserAgent,
    string IpAddress,
    bool IsActive,
    bool IsCurrentDevice,
    DateTime CreatedAt,
    DateTime LastRotatedAt,
    DateTime SlidingExpiresAt,
    DateTime AbsoluteExpiresAt,
    bool IsNearingAbsoluteExpiration,
    TimeSpan RemainingAbsoluteTime);