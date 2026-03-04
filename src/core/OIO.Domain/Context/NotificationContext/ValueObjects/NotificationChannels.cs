using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.NotificationContext.ValueObjects;

/// <summary>
/// Wraps the <c>channels</c> JSONB column on <c>user_notification_preferences</c>.
/// Stores channel opt-in state as raw JSON, e.g. {"sms": false, "push": true, "email": true}.
/// EF maps this via ComplexProperty against the single <c>channels</c> column (jsonb).
/// </summary>
public sealed class NotificationChannels : ValueObject
{
    private NotificationChannels() { }

    private NotificationChannels(string rawJson)
    {
        RawJson = rawJson;
    }

    /// <summary>Raw JSON string persisted in the <c>channels</c> column.</summary>
    public string RawJson { get; private set; }

    public static NotificationChannels Default =>
        new("{\"sms\": false, \"push\": true, \"email\": true}");

    public static NotificationChannels From(string rawJson)
        => new(string.IsNullOrWhiteSpace(rawJson)
            ? "{\"sms\": false, \"push\": true, \"email\": true}"
            : rawJson);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RawJson;
    }
}