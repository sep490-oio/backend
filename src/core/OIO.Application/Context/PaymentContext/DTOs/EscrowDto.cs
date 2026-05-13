namespace OIO.Application.Context.PaymentContext.DTOs;

public sealed record EscrowDto(
    Guid Id,
    Guid OrderId,
    string? OrderNumber,
    Guid BuyerId,
    string? BuyerDisplayName,
    Guid SellerId,
    string? SellerDisplayName,
    string? AuctionItemTitle,
    decimal Amount,
    string Currency,
    string Status,
    Guid? HoldTransactionId,
    DateTime CreatedAt,
    DateTime? ReleasedAt,
    DateTime? RefundedAt,
    string? HoldReason = null,
    IReadOnlyCollection<EscrowReleaseEventDto>? ReleaseEvents = null);
