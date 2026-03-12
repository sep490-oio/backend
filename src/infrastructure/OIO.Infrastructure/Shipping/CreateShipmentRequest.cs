namespace OIO.Infrastructure.Shipping;

public sealed class CreateShipmentRequest
{
    public required string ClientOrderCode { get; init; }

    // ── Recipient (to_* in carrier API) ──────────────────────────────────────
    public required string RecipientName     { get; init; }
    public required string RecipientPhone    { get; init; }
    public required string RecipientAddress  { get; init; }
    public required string RecipientWard     { get; init; }
    public required string RecipientDistrict { get; init; }
    public required string RecipientProvince { get; init; }
    /// <summary>GHN: {"district_id":3695,"ward_code":"90752"}. Null for GHTK.</summary>
    public string? RecipientCarrierAddressDataJson { get; init; }

    // ── Sender override (from_* in carrier API) ───────────────────────────────
    // For INBOUND: seller's address — carrier picks up FROM seller, delivers TO warehouse.
    // For OUTBOUND: null — adapter uses the configured shop/pick address automatically.
    public string? SenderName     { get; init; }
    public string? SenderPhone    { get; init; }
    public string? SenderAddress  { get; init; }
    public string? SenderWard     { get; init; }
    public string? SenderDistrict { get; init; }
    public string? SenderProvince { get; init; }
    /// <summary>GHN: {"district_id":...,"ward_code":"..."} for seller. Null when not overriding.</summary>
    public string? SenderCarrierAddressDataJson { get; init; }

    // ── Package ───────────────────────────────────────────────────────────────
    /// <summary>Weight in GRAMS — adapters normalise per carrier.</summary>
    public required int WeightGrams { get; init; }
    public int? LengthCm { get; init; }
    public int? WidthCm  { get; init; }
    public int? HeightCm { get; init; }

    // ── Financials ────────────────────────────────────────────────────────────
    public decimal InsuranceValue { get; init; }
    public decimal CodAmount      { get; init; }

    // ── Items ─────────────────────────────────────────────────────────────────
    public required IReadOnlyList<ShipmentItem> Items { get; init; }

    // ── GHN-specific ──────────────────────────────────────────────────────────
    /// <summary>"1" = shop pays, "2" = buyer pays. Null = default (shop pays).</summary>
    public string? GhnPaymentTypeId { get; init; }
    /// <summary>CHOTHUHANG | CHOXEMHANGKHONGTHU | KHONGCHOXEMHANG.</summary>
    public string? GhnHandlingNote  { get; init; }

    // ── GHTK-specific ────────────────────────────────────────────────────────
    public string GhtkPickOption { get; init; } = "cod";
    public string GhtkTransport  { get; init; } = "road";

    public string? ExtraDataJson { get; init; }
}