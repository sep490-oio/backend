namespace OIO.Application.Context.UserContext.DTOs;

public sealed record UserNotificationPreferenceDto(
    Guid Id,
    bool IsEnabled,
    string Channels,
    string? QuietHours,
    string? RateLimits,
    DateTime CreatedAt,
    DateTime? ModifiedAt);
