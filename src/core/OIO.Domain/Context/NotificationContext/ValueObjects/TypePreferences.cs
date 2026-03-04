using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.NotificationContext.ValueObjects;

/// <summary>
/// Wraps the <c>type_preferences</c> JSONB column on <c>user_notification_preferences</c>.
/// Allows users to opt in/out per notification type, e.g.:
/// { "order_update": true, "promo": false, "security_alert": true }
/// EF maps this via ComplexProperty against the single <c>type_preferences</c> column (jsonb).
/// </summary>
public sealed class TypePreferences : ValueObject
{
    private TypePreferences() { }

    private TypePreferences(string rawJson)
    {
        RawJson = rawJson;
    }

    /// <summary>Raw JSON string persisted in the <c>type_preferences</c> column.</summary>
    public string RawJson { get; private set; }

    /// <summary>No overrides — all types follow the global <c>is_enabled</c> flag.</summary>
    public static TypePreferences Empty => new("{}");

    public static TypePreferences From(string rawJson)
        => new(string.IsNullOrWhiteSpace(rawJson) ? "{}" : rawJson);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RawJson;
    }
}
