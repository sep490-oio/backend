namespace OIO.Application.Context.PaymentContext.DTOs;

public sealed record EscrowDetailDto(
    Guid Id,
    Guid OrderId,
    Guid BuyerId,
    Guid SellerId,
    decimal Amount,
    string Currency,
    string Status,
    Guid? HoldTransactionId,
    Guid? ReleaseTransactionId,
    string ReleasedTo,
    DateTime CreatedAt,
    DateTime? ReleasedAt,
    IReadOnlyList<EscrowReleaseEventDto> ReleaseEvents);
