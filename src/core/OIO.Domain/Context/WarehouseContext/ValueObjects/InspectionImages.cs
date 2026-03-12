using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.WarehouseContext.ValueObjects;

public sealed class InspectionImages : ValueObject
{
    private InspectionImages() { }

    private InspectionImages(string rawJson)
    {
        RawJson = rawJson;
    }

    public string RawJson { get; private set; }

    public static InspectionImages Empty => new("[]");

    public static InspectionImages From(string rawJson)
        => new(string.IsNullOrWhiteSpace(rawJson) ? "[]" : rawJson);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RawJson;
    }
}