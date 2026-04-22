namespace OIO.Application.Context.OrderContext.DTOs;

public sealed record OrderReturnDto(
    Guid Id,
    string Status,
    string ReasonCode,
    string? Description,
    string? DecisionReason,
    string? ProviderCode,
    string? TrackingNumber,
    DateTime RequestedAt,
    DateTime? ApprovedAt,
    DateTime? RejectedAt,
    DateTime? ShippedAt,
    DateTime? SellerReceivedAt,
    DateTime? BuyerDecisionDueAt,
    /// <summary>
    /// Signed return-scoped QR token — populated at Approve time (dispute flow
    /// and buyer-initiated approval). FE reads this to render the shipping
    /// label BEFORE the buyer hands the parcel to the carrier.
    /// </summary>
    string? QrToken,
    /// <summary>
    /// Chain-of-custody photos attached to this return. Populated when
    /// <c>GetOrderByIdQuery</c> includes <c>Return.Evidence</c>.
    /// </summary>
    IReadOnlyList<OrderReturnEvidenceDto> Evidence);
