using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.Context.UserContext.Aggregates.Users;

namespace OIO.Application.Context.UserContext.Mappings;

public static class UserNotificationPreferenceMappings
{
    public static UserNotificationPreferenceDto ToDto(this UserNotificationPreference preference)
    {
        return new UserNotificationPreferenceDto(
            Id: preference.Id.Value,
            IsEnabled: preference.IsEnabled,
            Channels: preference.Channels,
            QuietHours: preference.QuietHours,
            RateLimits: preference.RateLimits,
            CreatedAt: preference.CreatedAt,
            ModifiedAt: preference.ModifiedAt);
    }
}
