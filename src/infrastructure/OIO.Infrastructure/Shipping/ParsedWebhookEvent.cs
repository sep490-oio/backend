using OIO.Domain.Context.WarehouseContext.Enums;

namespace OIO.Infrastructure.Shipping;

public sealed class ParsedWebhookEvent
{
    public required string ProviderCode { get; init; }

    /// <summary>Our client_order_code — to look up the shipment by our reference.</summary>
    public string? ClientOrderCode { get; init; }

    /// <summary>Carrier tracking number — fallback lookup if ClientOrderCode is absent.</summary>
    public string? CarrierTrackingNumber { get; init; }

    /// <summary>
    /// Exactly what the carrier sent — never normalised before storing.
    /// GHN: string e.g. "delivered". GHTK: integer as string e.g. "3".
    /// </summary>
    public required string  CarrierStatusRaw  { get; init; }
    public          string? CarrierStatusDesc { get; init; }

    public required NormalizedTrackingStatus NormalizedStatus { get; init; }

    public string? Location          { get; init; }
    public string? ReasonCode        { get; init; }
    public string? ReasonDescription { get; init; }

    /// <summary>Carrier's action timestamp — not our received time.</summary>
    public required DateTime EventTime { get; init; }

    /// <summary>
    /// Full body already serialised to JSON.
    /// GHTK sends application/x-www-form-urlencoded — adapter converts to JSON before returning.
    /// </summary>
    public required string RawPayloadJson { get; init; }
}