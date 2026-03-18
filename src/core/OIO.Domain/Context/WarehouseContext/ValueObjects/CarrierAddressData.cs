using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.WarehouseContext.ValueObjects;

public sealed class CarrierAddressData : ValueObject
{
    private CarrierAddressData() { }

    private CarrierAddressData(string rawJson)
    {
        RawJson = rawJson;
    }

    public string RawJson { get; private set; }

    public static CarrierAddressData From(string rawJson)
        => new(string.IsNullOrWhiteSpace(rawJson) ? "{}" : rawJson);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RawJson;
    }
}
