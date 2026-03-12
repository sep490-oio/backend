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
    string  RecipientName,
    string  RecipientPhone,
    string  RecipientAddress,
    string  RecipientWard,
    string  RecipientDistrict,
    string  RecipientProvince,
    // ── Package ────────────────────────────────────────────────────────────
    int     WeightGrams,
    decimal InsuranceValue,
    decimal CodAmount,
    // ── Item info for carrier manifest ────────────────────────────────────
    string  ItemName,
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
    string? ExtraDataJson                   = null
) : ICommand<OutboundShipmentDto>, IHasValidate
{
    public ViolationsError Validate() =>
        BookOutboundShipmentCommand.Check()
            .WithOwnerName("BookOutboundShipment")
            .Field(OrderId).NotEmptyGuid()
            .Field(WarehouseItemId).NotEmptyGuid()
            .Field(RecipientName).NotWhiteSpace()
            .Field(RecipientPhone).NotWhiteSpace()
            .Field(RecipientAddress).NotWhiteSpace()
            .Field(RecipientWard).NotWhiteSpace()
            .Field(RecipientDistrict).NotWhiteSpace()
            .Field(RecipientProvince).NotWhiteSpace()
            .Field(WeightGrams).GreaterThan(0)
            .Field(InsuranceValue).NonNegative()
            .Field(CodAmount).NonNegative()
            .Field(ItemName).NotWhiteSpace()
            .Field(ItemPrice).NonNegative();
}