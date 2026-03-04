using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.NotificationContext.ValueObjects;

/// <summary>
/// Wraps the <c>metadata</c> JSONB column on the <c>notifications</c> table.
/// Stores arbitrary key/value context about the notification (e.g. order id, product id).
/// EF maps this via ComplexProperty against a single <c>metadata</c> column (jsonb).
/// </summary>
public sealed class NotificationMetadata : ValueObject
{
    private NotificationMetadata() { }

    private NotificationMetadata(string rawJson)
    {
        RawJson = rawJson;
    }

    /// <summary>Raw JSON string persisted in the <c>metadata</c> column.</summary>
    public string RawJson { get; private set; }

    public static NotificationMetadata Empty => new("{}");

    public static NotificationMetadata From(string rawJson)
        => new(string.IsNullOrWhiteSpace(rawJson) ? "{}" : rawJson);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RawJson;
    }
}
