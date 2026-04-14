using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Commands.BookInboundShipment; // Reuse GhnMetadata record
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.BookOutboundShipment;

/// <summary>
/// Specialized command for GHN outbound booking that includes specific DistrictID and WardCode metadata.
/// Matches the original BookOutboundShipmentCommand fields for parity.
/// </summary>
public sealed record BookGhnOutboundShipmentCommand(
    Guid    OrderId,
    Guid    WarehouseItemId,
    // ── Buyer (recipient) address ──────────────────────────────────────────
    string? RecipientName           = null,
    string? RecipientPhone          = null,
    string? RecipientAddress        = null,
    string? RecipientWard           = null,
    string? RecipientDistrict       = null,
    string? RecipientProvince       = null,
    GhnMetadata? RecipientMetadata  = null,
    // ── Sender (pickup) address ─────────────────────────────────────────────
    string? SenderName              = null,
    string? SenderPhone             = null,
    string? SenderAddress           = null,
    string? SenderWard              = null,
    string? SenderDistrict          = null,
    string? SenderProvince          = null,
    GhnMetadata? SenderMetadata     = null,
    // ── Package ────────────────────────────────────────────────────────────
    int     WeightGrams             = 0,
    decimal InsuranceValue          = 0,
    decimal CodAmount               = 0,
    // ── Item info for carrier manifest ────────────────────────────────────
    string? ItemName                = null,
    decimal ItemPrice               = 0,
    // ── Optional/Specialized ──────────────────────────────────────────────
    int?    LengthCm                = null,
    int?    WidthCm                 = null,
    int?    HeightCm                = null,
    string? RecipientCarrierAddressDataJson = null,
    string? GhnPaymentTypeId        = null,
    string? GhnHandlingNote         = null,
    string? ShippingMethod          = null,
    string? ExtraDataJson           = null,
    // ── Shipment mode (parity with original) ──────────────────────────────
    string? ShipmentMode            = null,
    string? ExternalCarrierName     = null,
    string? CarrierTrackingNumber   = null,
    // ── Evidence photos ──────────────────────────────────────────────────
    List<Guid>? PackagePhotoMediaUploadIds  = null,
    List<Guid>? HandoverPhotoMediaUploadIds = null
) : ICommand<OutboundShipmentDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        var check = BookGhnOutboundShipmentCommand.Check()
            .WithOwnerName("BookGhnOutboundShipment")
            .Field(OrderId).NotEmptyGuid()
            .Field(WarehouseItemId).NotEmptyGuid()
            .Field(WeightGrams).NonNegative()
            .Field(InsuranceValue).NonNegative()
            .Field(CodAmount).NonNegative()
            .Field(ItemPrice).NonNegative();

        if (SenderMetadata != null)
        {
            check.Field(SenderMetadata.Id, "SenderMetadata.Id").GreaterThan(0);
            check.Field(SenderMetadata.Code, "SenderMetadata.Code").NotNullOrWhiteSpace();
        }

        if (RecipientMetadata != null)
        {
            check.Field(RecipientMetadata.Id, "RecipientMetadata.Id").GreaterThan(0);
            check.Field(RecipientMetadata.Code, "RecipientMetadata.Code").NotNullOrWhiteSpace();
        }

        return check;
    }
}
