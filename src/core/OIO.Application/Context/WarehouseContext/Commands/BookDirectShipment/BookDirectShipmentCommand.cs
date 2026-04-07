using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.BookDirectShipment;

/// <summary>
/// Used for direct seller-to-buyer GHN booking (when item is not in warehouse).
/// </summary>
public sealed record BookDirectShipmentCommand(
    Guid    OrderId,
    // ── Package ────────────────────────────────────────────────────────────
    int     WeightGrams,
    decimal InsuranceValue,
    // ── Optional/Overrides ────────────────────────────────────────────────
    string? SenderName      = null,
    string? SenderPhone     = null,
    string? SenderAddress   = null,
    string? SenderWard      = null,
    string? SenderDistrict  = null,
    string? SenderProvince  = null,
    string? SenderCarrierAddressDataJson = null,
    string? RecipientCarrierAddressDataJson = null,
    // ── Item info for carrier manifest ────────────────────────────────────
    string? ItemName  = null,
    decimal? ItemPrice = null,
    // ── GHN specific ──────────────────────────────────────────────────────
    string? GhnPaymentTypeId = null,
    string? GhnHandlingNote  = null,
    string? ShippingMethod   = null,
    int?    LengthCm         = null,
    int?    WidthCm          = null,
    int?    HeightCm         = null
) : ICommand<OutboundShipmentDto>, IHasValidate
{
    public ViolationsError Validate() =>
        BookDirectShipmentCommand.Check()
            .WithOwnerName("BookDirectShipment")
            .Field(OrderId).NotEmptyGuid()
            .Field(WeightGrams).GreaterThan(0)
            .Field(InsuranceValue).NonNegative();
}
