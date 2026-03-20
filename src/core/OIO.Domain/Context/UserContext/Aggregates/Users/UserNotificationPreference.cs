using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.UserContext.Aggregates.Users;

public sealed class UserNotificationPreference : BaseEntity<UserNotificationPreferenceId>, IAuditableEntity
{
    public UserId UserId { get; private set; }
    public bool IsEnabled { get; private set; }
    public string TypePreferences { get; private set; }  // jsonb
    public string Channels { get; private set; }          // jsonb
    public string? QuietHours { get; private set; }       // jsonb
    public string? RateLimits { get; private set; }       // jsonb
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    private UserNotificationPreference() { }

    public static UserNotificationPreference Create(UserId userId, DateTime nowUtc)
    {
        return new UserNotificationPreference
        {
            Id = UserNotificationPreferenceId.From(Guid.NewGuid()),
            UserId = userId,
            IsEnabled = true,
            TypePreferences = "{}",
            Channels = "{}",
            CreatedAt = nowUtc
        };
    }

    public void Update(bool isEnabled, string channels, string? quietHours, DateTime nowUtc)
    {
        IsEnabled = isEnabled;
        Channels = channels;
        QuietHours = quietHours;
        ModifiedAt = nowUtc;
    }
}