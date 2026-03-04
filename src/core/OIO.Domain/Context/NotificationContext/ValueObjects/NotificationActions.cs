using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.NotificationContext.ValueObjects;

/// <summary>
/// Wraps the <c>actions</c> JSONB column. Stored as a serialized JSON string.
/// Exposed as a rich collection of <see cref="NotificationAction"/> items.
/// EF maps this via ComplexProperty against a single <c>actions</c> column (jsonb).
/// </summary>
public sealed class NotificationActions : ValueObject
{
    private NotificationActions() { }

    private NotificationActions(string rawJson, IReadOnlyList<NotificationAction> items)
    {
        RawJson = rawJson;
        Items = items;
    }

    /// <summary>Raw JSON string persisted in the <c>actions</c> column.</summary>
    public string RawJson { get; private set; }

    /// <summary>Parsed action items — not persisted, derived from <see cref="RawJson"/>.</summary>
    public IReadOnlyList<NotificationAction> Items { get; private set; }

    public static NotificationActions Empty =>
        new("[]", Array.Empty<NotificationAction>());

    public static NotificationActions From(string rawJson, IReadOnlyList<NotificationAction> items)
        => new(rawJson, items);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RawJson;
    }
}
