namespace OIO.Infrastructure.Shipping;


public sealed class ShipmentItem
{
    public required string  Name        { get; init; }
    public required string  Code        { get; init; }
    public required int     Quantity    { get; init; }
    public required decimal Price       { get; init; }
    /// <summary>Weight in GRAMS — adapter normalises per carrier.</summary>
    public required int     WeightGrams { get; init; }
    public int? LengthCm { get; init; }
    public int? WidthCm  { get; init; }
    public int? HeightCm { get; init; }
}