namespace OIO.Infrastructure.Shipping;

public sealed class CalculateFeeRequest
{
    public required int     WeightGrams       { get; init; }
    public required decimal InsuranceValue    { get; init; }
    public required decimal CodAmount         { get; init; }
    public required string  RecipientDistrict { get; init; }
    public required string  RecipientProvince { get; init; }
    /// <summary>GHN: required ward_code + district_id JSON. GHTK: not needed.</summary>
    public string? RecipientCarrierAddressDataJson { get; init; }
    public int? LengthCm { get; init; }
    public int? WidthCm  { get; init; }
    public int? HeightCm { get; init; }
}
