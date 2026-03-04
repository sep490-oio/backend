using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.NotificationContext.ValueObjects;

/// <summary>
/// Wraps the <c>rate_limits</c> JSONB column on <c>user_notification_preferences</c>.
/// Stores rate limit configuration as raw JSON.
/// EF maps this via ComplexProperty against the single <c>rate_limits</c> column (jsonb).
/// </summary>
public sealed class NotificationRateLimits : ValueObject
{
    private NotificationRateLimits() { }

    private NotificationRateLimits(string rawJson)
    {
        RawJson = rawJson;
    }

    /// <summary>Raw JSON string persisted in the <c>rate_limits</c> column.</summary>
    public string RawJson { get; private set; }

    public static NotificationRateLimits From(string rawJson)
        => new(string.IsNullOrWhiteSpace(rawJson) ? "{}" : rawJson);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RawJson;
    }
}