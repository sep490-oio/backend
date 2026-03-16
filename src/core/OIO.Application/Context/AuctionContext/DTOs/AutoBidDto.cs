namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record AutoBidDto(
    Guid Id,
    Guid AuctionId,
    Guid BidderId,
    bool IsEnabled,
    MoneyDto MaxAmount,
    MoneyDto CurrentAmount,
    MoneyDto ReservedAmount,
    MoneyDto? IncrementAmount,
    string Status,
    int TotalAutoBids,
    DateTime? LastAutoBidAt,
    string? StopReason,
    DateTime? StoppedAt,
    DateTime? LastValidationAt,
    DateTime CreatedAt);
