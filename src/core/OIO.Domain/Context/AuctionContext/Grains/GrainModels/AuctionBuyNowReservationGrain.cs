using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Grains.GrainValueObjects;

namespace OIO.Domain.Context.AuctionContext.Grains.GrainModels;

[GenerateSerializer]
[Alias("OIO.Domain.Context.AuctionContext.Grains.GrainModels.AuctionBuyNowReservationGrain")]
public sealed record AuctionBuyNowReservationGrain(
    Guid Id,
    Guid AuctionId,
    Guid BuyerId,
    MoneyGrain BuyNowPrice,
    MoneyGrain DepositAppliedAmount,
    MoneyGrain GatewayAmountDue,
    string Status,
    DateTime ExpiresAt,
    Guid? PaymentTransactionId,
    Guid? OrderId,
    DateTime CreatedAt)
{
    public static AuctionBuyNowReservationGrain From(AuctionBuyNowReservation reservation) =>
        new(
            Id: reservation.Id.Value,
            AuctionId: reservation.AuctionId.Value,
            BuyerId: reservation.BuyerId.Value,
            BuyNowPrice: MoneyGrain.From(reservation.BuyNowPrice),
            DepositAppliedAmount: MoneyGrain.From(reservation.DepositAppliedAmount),
            GatewayAmountDue: MoneyGrain.From(reservation.GatewayAmountDue),
            Status: reservation.Status.Id,
            ExpiresAt: reservation.ExpiresAt,
            PaymentTransactionId: reservation.PaymentTransactionId?.Value,
            OrderId: reservation.OrderId?.Value,
            CreatedAt: reservation.CreatedAt);
}
