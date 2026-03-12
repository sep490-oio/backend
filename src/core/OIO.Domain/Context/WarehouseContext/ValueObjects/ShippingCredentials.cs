using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.WarehouseContext.ValueObjects;

public sealed class ShippingCredentials : ValueObject
{
    private ShippingCredentials() { }

    private ShippingCredentials(string rawJson)
    {
        RawJson = rawJson;
    }

    public string RawJson { get; private set; }

    public static ShippingCredentials From(string rawJson)
        => new(string.IsNullOrWhiteSpace(rawJson) ? "{}" : rawJson);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RawJson;
    }
}