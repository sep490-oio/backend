namespace OIO.Infrastructure.Shipping;

public sealed class CreateShipmentResponse
{
    /// <summary>
    /// Carrier's tracking number.
    /// GHN: order_code. GHTK: label.
    /// </summary>
    public required string   CarrierTrackingNumber { get; init; }
    public          string?  ShippingLabelUrl      { get; init; }
    public          decimal  ShippingFee           { get; init; }
    public          DateTime? EstimatedDeliveryAt  { get; init; }
}