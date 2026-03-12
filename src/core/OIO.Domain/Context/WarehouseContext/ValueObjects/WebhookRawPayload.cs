using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.WarehouseContext.ValueObjects;

public sealed class WebhookRawPayload : ValueObject
{
    private WebhookRawPayload() { }

    private WebhookRawPayload(string rawJson)
    {
        RawJson = rawJson;
    }

    public string RawJson { get; private set; }

    public static WebhookRawPayload From(string rawJson)
        => new(string.IsNullOrWhiteSpace(rawJson) ? "{}" : rawJson);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RawJson;
    }
}