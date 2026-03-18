using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.AuctionContext.Enums;

public sealed class BuyNowReservationStatus : EnumValueObject<BuyNowReservationStatus>
{
    public static readonly BuyNowReservationStatus PendingPayment = new("pending_payment");
    public static readonly BuyNowReservationStatus Paid = new("paid");
    public static readonly BuyNowReservationStatus Expired = new("expired");
    public static readonly BuyNowReservationStatus Cancelled = new("cancelled");
    public static readonly BuyNowReservationStatus Failed = new("failed");

    private BuyNowReservationStatus(string id) : base(id) { }

    public bool IsTerminal =>
        this == Paid ||
        this == Expired ||
        this == Cancelled ||
        this == Failed;
}
