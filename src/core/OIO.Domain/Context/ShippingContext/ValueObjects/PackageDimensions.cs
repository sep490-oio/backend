using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ShippingContext.ValueObjects;

public sealed class PackageDimensions : ValueObject
{
    public int WeightGrams { get; }
    public int? LengthCm { get; }
    public int? WidthCm { get; }
    public int? HeightCm { get; }

    public PackageDimensions()
    {
        
    }

    private PackageDimensions(
        int weightGrams,
        int? lengthCm,
        int? widthCm,
        int? heightCm)
    {
        WeightGrams = weightGrams;
        LengthCm = lengthCm;
        WidthCm = widthCm;
        HeightCm = heightCm;
    }

    public static PackageDimensions Create(
        int weightGrams, 
        int? lengthCm = null,
        int? widthCm = null,
        int? heightCm = null)
        => new(weightGrams, lengthCm, widthCm, heightCm);

    /// <summary>Volume in cm³, null if any dimension missing</summary>
    public int? VolumeCm3 => LengthCm.HasValue && WidthCm.HasValue && HeightCm.HasValue
        ? LengthCm.Value * WidthCm.Value * HeightCm.Value
        : null;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return WeightGrams;
        yield return LengthCm ?? 0;
        yield return WidthCm ?? 0;
        yield return HeightCm ?? 0;
    }
}