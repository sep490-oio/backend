using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.WarehouseContext.ValueObjects;

public sealed class ShipmentExtraData : ValueObject
{
    private ShipmentExtraData() { }

    private ShipmentExtraData(string rawJson)
    {
        RawJson = rawJson;
    }

    public string RawJson { get; private set; }

    public static ShipmentExtraData Empty => new("{}");

    public static ShipmentExtraData From(string rawJson)
        => new(string.IsNullOrWhiteSpace(rawJson) ? "{}" : rawJson);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RawJson;
    }
}
