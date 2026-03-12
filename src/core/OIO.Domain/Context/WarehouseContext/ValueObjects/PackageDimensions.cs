using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.WarehouseContext.ValueObjects;

public sealed class PackageDimensions : ValueObject
{
    private PackageDimensions() { }

    private PackageDimensions(int weightGrams, int? lengthCm, int? widthCm, int? heightCm)
    {
        WeightGrams = weightGrams;
        LengthCm    = lengthCm;
        WidthCm     = widthCm;
        HeightCm    = heightCm;
    }

    public int  WeightGrams { get; private set; }
    public int? LengthCm   { get; private set; }
    public int? WidthCm    { get; private set; }
    public int? HeightCm   { get; private set; }

    /// <summary>Weight in kilograms for carriers that require kg (e.g. GHTK).</summary>
    public decimal WeightKg => WeightGrams / 1000m;

    public static Result<PackageDimensions, Error> Create(
        int weightGrams,
        int? lengthCm  = null,
        int? widthCm   = null,
        int? heightCm  = null)
    {
        if (weightGrams <= 0)
            return Error.Validation(
                propertyName: nameof(WeightGrams),
                code: "PackageDimensions.WeightGrams.Invalid",
                description: "Weight must be greater than zero.");

        return new PackageDimensions(weightGrams, lengthCm, widthCm, heightCm);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return WeightGrams;
        yield return LengthCm;
        yield return WidthCm;
        yield return HeightCm;
    }
}
