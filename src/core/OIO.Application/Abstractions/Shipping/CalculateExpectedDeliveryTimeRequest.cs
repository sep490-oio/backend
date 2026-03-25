namespace OIO.Application.Abstractions.Shipping;

public sealed class CalculateExpectedDeliveryTimeRequest
{
    public required string SenderDistrict { get; init; }
    public required string SenderProvince { get; init; }
    public string? SenderCarrierAddressDataJson { get; init; }

    public required string RecipientDistrict { get; init; }
    public required string RecipientProvince { get; init; }
    public string? RecipientCarrierAddressDataJson { get; init; }
}
