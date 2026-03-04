using CSharpFunctionalExtensions;
using OIO.Domain.Context.NotificationContext.ValueObjects;
using OIO.Domain.Context.NotificationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;
using e = OIO.Domain.SeedWork.Errors.Error;

namespace OIO.Domain.Context.NotificationContext.Aggregates.UserNotificationPreferences;

public sealed class UserNotificationPreference : AggregateRoot<UserNotificationPreferenceId>, IAuditableEntity
{
#pragma warning disable CS8618
    private UserNotificationPreference() { }
#pragma warning restore CS8618

    public UserId UserId { get; private set; }

    /// <summary>
    /// Master switch — when false, no notifications are delivered regardless of other settings.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>Per-notification-type opt-in/out overrides.</summary>
    public TypePreferences TypePreferences { get; private set; }

    /// <summary>Which delivery channels the user has opted into.</summary>
    public NotificationChannels Channels { get; private set; }

    /// <summary>Time window during which delivery is suppressed. Null means no quiet hours.</summary>
    public QuietHours? QuietHours { get; private set; }

    /// <summary>Per-user delivery rate cap. Null means no user-level cap enforced.</summary>
    public NotificationRateLimits? RateLimits { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? ModifiedAt { get; private set; }

    private UserNotificationPreference(
        UserNotificationPreferenceId id,
        UserId userId,
        DateTime createdAt)
    {
        Id = id;
        UserId = userId;
        IsEnabled = true;
        TypePreferences = TypePreferences.Empty;
        Channels = NotificationChannels.Default;
        CreatedAt = createdAt;
    }

    public static UserNotificationPreference Create(UserId userId, DateTime now)
    {
        return new UserNotificationPreference(
            UserNotificationPreferenceId.From(Guid.CreateVersion7()),
            userId,
            now);
    }

    public void Enable(DateTime now)
    {
        if (IsEnabled) return;

        IsEnabled = true;
        ModifiedAt = now;
    }

    public void Disable(DateTime now)
    {
        if (!IsEnabled) return;

        IsEnabled = false;
        ModifiedAt = now;
    }

    public void UpdateChannels(NotificationChannels channels, DateTime now)
    {
        Channels = channels;
        ModifiedAt = now;
    }

    public void SetQuietHours(QuietHours quietHours, DateTime now)
    {
        QuietHours = quietHours;
        ModifiedAt = now;
    }

    public void ClearQuietHours(DateTime now)
    {
        QuietHours = null;
        ModifiedAt = now;
    }

    public UnitResult<e> SetRateLimits(NotificationRateLimits rateLimits, DateTime now)
    {
        RateLimits = rateLimits;
        ModifiedAt = now;
        return UnitResult.Success<e>();
    }

    public void ClearRateLimits(DateTime now)
    {
        RateLimits = null;
        ModifiedAt = now;
    }

    public void UpdateTypePreferences(TypePreferences preferences, DateTime now)
    {
        TypePreferences = preferences;
        ModifiedAt = now;
    }
}