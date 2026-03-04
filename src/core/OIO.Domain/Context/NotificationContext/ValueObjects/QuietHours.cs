using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.NotificationContext.ValueObjects;

/// <summary>
/// Wraps the <c>quiet_hours</c> JSONB column on <c>user_notification_preferences</c>.
/// Stores quiet hours configuration as raw JSON.
/// EF maps this via ComplexProperty against the single <c>quiet_hours</c> column (jsonb).
/// </summary>
public sealed class QuietHours : ValueObject
{
    private QuietHours() { }

    private QuietHours(string rawJson)
    {
        RawJson = rawJson;
    }

    /// <summary>Raw JSON string persisted in the <c>quiet_hours</c> column.</summary>
    public string RawJson { get; private set; }

    public static QuietHours From(string rawJson)
        => new(string.IsNullOrWhiteSpace(rawJson) ? "{}" : rawJson);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RawJson;
    }
}