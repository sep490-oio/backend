using CSharpFunctionalExtensions;
using MediatR;
using OIO.Domain.Context.WarehouseContext.Aggregates.ShippingProviders;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Abstractions.Shipping;

// ── Application-layer DTOs ────────────────────────────────────────────────────
// These are NOT the same as the Infrastructure CreateShipmentRequest/Response.
// Application layer cannot reference OIO.Infrastructure.Shipping.

public sealed class BookShipmentItem
{
    public required string  Name        { get; init; }
    public required string  Code        { get; init; }
    public required int     Quantity    { get; init; }
    public required decimal Price       { get; init; }
    public required int     WeightGrams { get; init; }
}

public sealed class BookShipmentRequest
{
    public required string ClientOrderCode { get; init; }

    // ── Recipient (where package is delivered TO) ────────────────────────────
    public required string RecipientName    { get; init; }
    public required string RecipientPhone   { get; init; }
    public required string RecipientAddress { get; init; }
    public required string RecipientWard    { get; init; }
    public required string RecipientDistrict { get; init; }
    public required string RecipientProvince { get; init; }
    /// <summary>
    /// GHN: {"district_id":3695,"ward_code":"90752"}
    /// Null for GHTK or when using plain text is sufficient.
    /// </summary>
    public string? RecipientCarrierAddressDataJson { get; init; }

    // ── Sender override (when pickup is NOT the default shop/config address) ─
    // For INBOUND: seller's address (carrier picks up FROM seller → delivers TO warehouse).
    // For OUTBOUND: leave null → adapter uses the configured shop/pick address.
    public string? SenderName     { get; init; }
    public string? SenderPhone    { get; init; }
    public string? SenderAddress  { get; init; }
    public string? SenderWard     { get; init; }
    public string? SenderDistrict { get; init; }
    public string? SenderProvince { get; init; }
    /// <summary>GHN: {"district_id":...,"ward_code":"..."} for seller's address. Null for GHTK.</summary>
    public string? SenderCarrierAddressDataJson { get; init; }

    // ── Package ───────────────────────────────────────────────────────────────
    public required int WeightGrams { get; init; }
    public int? LengthCm { get; init; }
    public int? WidthCm  { get; init; }
    public int? HeightCm { get; init; }

    // ── Financials ────────────────────────────────────────────────────────────
    public decimal InsuranceValue { get; init; }
    public decimal CodAmount      { get; init; }

    // ── Items ─────────────────────────────────────────────────────────────────
    public required IReadOnlyList<BookShipmentItem> Items { get; init; }

    // ── Carrier-specific ──────────────────────────────────────────────────────
    /// <summary>GHN: "1" = shop pays, "2" = buyer pays. Null = default (shop pays).</summary>
    public string? GhnPaymentTypeId { get; init; }
    /// <summary>GHN required_note: CHOTHUHANG | CHOXEMHANGKHONGTHU | KHONGCHOXEMHANG.</summary>
    public string? GhnHandlingNote  { get; init; }
    public string? ExtraDataJson    { get; init; }
}

public sealed class ShipmentBookingResult
{
    public required string   CarrierTrackingNumber { get; init; }
    public          string?  ShippingLabelUrl      { get; init; }
    public          decimal  ShippingFee           { get; init; }
    public          DateTime? EstimatedDeliveryAt  { get; init; }
}

// ── Interface ─────────────────────────────────────────────────────────────────

/// <summary>
/// Application-layer abstraction for carrier operations.
/// Implemented in Infrastructure by ShippingService → IShippingProviderSelector.
/// Command handlers inject this — never IShippingProvider directly.
/// </summary>
public interface IShippingService
{
    /// <summary>
    /// Books a shipment with the named carrier and returns the booking result.
    /// providerCode must match a registered IShippingProvider ("ghn", "ghtk").
    /// </summary>
    Task<Result<ShipmentBookingResult, Error>> BookShipmentAsync(
        string                 providerCode,
        BookShipmentRequest    request,
        ShippingProviderConfig config,
        CancellationToken      ct = default);

    /// <summary>Cancels a previously booked shipment. Only valid before pickup.</summary>
    Task<Result<Unit, Error>> CancelShipmentAsync(
        string                 providerCode,
        string                 carrierTrackingNumber,
        ShippingProviderConfig config,
        CancellationToken      ct = default);
}