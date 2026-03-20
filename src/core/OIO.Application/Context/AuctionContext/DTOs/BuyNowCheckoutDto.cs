namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record BuyNowCheckoutDto(
    Guid ReservationId,
    string PaymentUrl,
    DateTime ExpiresAt,
    MoneyDto BuyNowPrice,
    MoneyDto DepositAppliedAmount,
    MoneyDto AmountDue);
