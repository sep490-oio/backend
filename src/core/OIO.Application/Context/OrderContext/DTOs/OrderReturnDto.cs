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
    DateTime? BuyerDecisionDueAt);
