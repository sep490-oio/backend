using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.NotificationContext.ValueObjects;

/// <summary>
/// Wraps the <c>related_entities</c> JSONB column.
/// Stores a list of entity references associated with the notification,
/// e.g. [{ "type": "order", "id": "..." }, { "type": "product", "id": "..." }].
/// EF maps this via ComplexProperty against a single <c>related_entities</c> column (jsonb).
/// </summary>
public sealed class RelatedEntities : ValueObject
{
    private RelatedEntities() { }

    private RelatedEntities(string rawJson)
    {
        RawJson = rawJson;
    }

    /// <summary>Raw JSON string persisted in the <c>related_entities</c> column.</summary>
    public string RawJson { get; private set; }

    public static RelatedEntities Empty => new("[]");

    public static RelatedEntities From(string rawJson)
        => new(string.IsNullOrWhiteSpace(rawJson) ? "[]" : rawJson);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RawJson;
    }
}
