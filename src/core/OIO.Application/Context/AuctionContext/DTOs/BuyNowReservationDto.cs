namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record BuyNowReservationDto(
    Guid ReservationId,
    Guid OrderId,
    DateTime ExpiresAt,
    MoneyDto BuyNowPrice,
    MoneyDto DepositAppliedAmount,
    MoneyDto AmountDue);
