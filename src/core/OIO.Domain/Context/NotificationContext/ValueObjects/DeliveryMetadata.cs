using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.NotificationContext.ValueObjects;

/// <summary>
/// Wraps the <c>delivery_metadata</c> JSONB column on <c>notification_delivery</c>.
/// Stores channel-specific delivery context, e.g. provider message IDs,
/// FCM tokens, SMTP response codes, SMS gateway references.
/// EF maps this via ComplexProperty against a single <c>delivery_metadata</c> column (jsonb).
/// </summary>
public sealed class DeliveryMetadata : ValueObject
{
    private DeliveryMetadata() { }

    private DeliveryMetadata(string rawJson)
    {
        RawJson = rawJson;
    }

    /// <summary>Raw JSON string persisted in the <c>delivery_metadata</c> column.</summary>
    public string RawJson { get; private set; }

    public static DeliveryMetadata Empty => new("{}");

    public static DeliveryMetadata From(string rawJson)
        => new(string.IsNullOrWhiteSpace(rawJson) ? "{}" : rawJson);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RawJson;
    }
}
