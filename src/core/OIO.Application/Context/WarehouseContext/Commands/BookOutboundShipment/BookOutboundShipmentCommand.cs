using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.BookOutboundShipment;

public sealed record BookOutboundShipmentCommand(
    Guid    OrderId,
    Guid    WarehouseItemId,
    // ── Buyer (recipient) address ──────────────────────────────────────────
    // All recipient fields may be empty — handler falls back to the order's
    // ShippingSnapshot when the FE omits them.
    string? RecipientName,
    string? RecipientPhone,
    string? RecipientAddress,
    string? RecipientWard,
    string? RecipientDistrict,
    string? RecipientProvince,
    // ── Package ────────────────────────────────────────────────────────────
    // Zero / null → handler uses detail DTO defaults.
    int     WeightGrams,
    decimal InsuranceValue,
    decimal CodAmount,
    // ── Item info for carrier manifest ────────────────────────────────────
    // Null / 0 → handler falls back to order item title and OrderPricing.ItemPrice.
    string? ItemName,
    decimal ItemPrice,
    // ── Optional ──────────────────────────────────────────────────────────
    int?    LengthCm                        = null,
    int?    WidthCm                         = null,
    int?    HeightCm                        = null,
    string? RecipientCarrierAddressDataJson = null,
    string? ProviderCode                    = null,  // null = use default active provider
    string? GhnPaymentTypeId                = null,  // "1" = shop pays, "2" = buyer pays
    string? GhnHandlingNote                 = null,  // CHOTHUHANG | CHOXEMHANGKHONGTHU | KHONGCHOXEMHANG
    string? ShippingMethod                  = null,
    string? ExtraDataJson                   = null,
    // ── Shipment mode (v1: platform_managed | external_carrier) ───────────
    // Null defaults to "platform_managed" (integrated carrier flow).
    string? ShipmentMode                    = null,
    string? ExternalCarrierName             = null,
    string? CarrierTrackingNumber           = null,
    // ── Evidence photos ──────────────────────────────────────────────────
    // At least one package photo is required.
    List<Guid>? PackagePhotoMediaUploadIds  = null,
    List<Guid>? HandoverPhotoMediaUploadIds = null
) : ICommand<OutboundShipmentDto>, IHasValidate
{
    public ViolationsError Validate() =>
        BookOutboundShipmentCommand.Check()
            .WithOwnerName("BookOutboundShipment")
            .Field(OrderId).NotEmptyGuid()
            .Field(WarehouseItemId).NotEmptyGuid()
            .Field(WeightGrams).NonNegative()
            .Field(InsuranceValue).NonNegative()
            .Field(CodAmount).NonNegative()
            .Field(ItemPrice).NonNegative()
            .Field(PackagePhotoMediaUploadIds?.Count ?? 0, nameof(PackagePhotoMediaUploadIds)).GreaterThan(0);
}