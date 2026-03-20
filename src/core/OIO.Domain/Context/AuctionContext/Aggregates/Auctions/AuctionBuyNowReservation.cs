using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.Enums;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class AuctionBuyNowReservation : BaseEntity<AuctionBuyNowReservationId>, IAuditableEntity
{
    public AuctionId AuctionId { get; private set; }
    public UserId BuyerId { get; private set; }
    public decimal BuyNowAmount { get; private set; }
    public decimal DepositAppliedAmountValue { get; private set; }
    public decimal GatewayAmountDueValue { get; private set; }
    public Currency Currency { get; private set; }
    public TransactionId? PaymentTransactionId { get; private set; }
    public OrderId? OrderId { get; private set; }
    public BuyNowReservationStatus Status { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    // Navigation
    public Auction Auction { get; private set; } = null!;

    public Money BuyNowPrice => Money.Of(BuyNowAmount, Currency);
    public Money DepositAppliedAmount => Money.Of(DepositAppliedAmountValue, Currency);
    public Money GatewayAmountDue => Money.Of(GatewayAmountDueValue, Currency);

    private AuctionBuyNowReservation() { }

    private AuctionBuyNowReservation(
        AuctionBuyNowReservationId id,
        AuctionId auctionId,
        UserId buyerId,
        Money buyNowPrice,
        Money depositAppliedAmount,
        Money gatewayAmountDue,
        DateTime expiresAt,
        DateTime nowUtc)
        : base(id)
    {
        AuctionId = auctionId;
        BuyerId = buyerId;
        BuyNowAmount = buyNowPrice.Amount;
        DepositAppliedAmountValue = depositAppliedAmount.Amount;
        GatewayAmountDueValue = gatewayAmountDue.Amount;
        Currency = buyNowPrice.Currency;
        Status = BuyNowReservationStatus.PendingPayment;
        ExpiresAt = expiresAt;
        CreatedAt = nowUtc;
        ModifiedAt = nowUtc;
    }

    public static Result<AuctionBuyNowReservation, Error> Create(
        AuctionId auctionId,
        UserId buyerId,
        Money buyNowPrice,
        Money depositAppliedAmount,
        Money gatewayAmountDue,
        DateTime expiresAt,
        DateTime nowUtc)
    {
        if (auctionId.Value == Guid.Empty)
            return AuctionErrors.BuyNowReservation.InvalidInput("AuctionId cannot be empty.");

        if (buyerId.Value == Guid.Empty)
            return AuctionErrors.BuyNowReservation.InvalidInput("BuyerId cannot be empty.");

        buyNowPrice.EnsureSameCurrency(depositAppliedAmount);
        buyNowPrice.EnsureSameCurrency(gatewayAmountDue);

        if (depositAppliedAmount.Amount < 0 || gatewayAmountDue.Amount < 0)
            return AuctionErrors.BuyNowReservation.InvalidInput("Reservation amounts must be non-negative.");

        if (depositAppliedAmount.Amount + gatewayAmountDue.Amount != buyNowPrice.Amount)
            return AuctionErrors.BuyNowReservation.InvalidFundingSplit;

        if (expiresAt <= nowUtc)
            return AuctionErrors.BuyNowReservation.InvalidExpiration;

        return new AuctionBuyNowReservation(
            AuctionBuyNowReservationId.From(Guid.CreateVersion7()),
            auctionId,
            buyerId,
            buyNowPrice,
            depositAppliedAmount,
            gatewayAmountDue,
            expiresAt,
            nowUtc);
    }

    public bool IsPendingPayment => Status == BuyNowReservationStatus.PendingPayment;

    public bool IsActive(DateTime nowUtc) =>
        Status == BuyNowReservationStatus.PendingPayment &&
        ExpiresAt > nowUtc;

    public UnitResult<Error> AttachPayment(TransactionId paymentTransactionId, DateTime nowUtc)
    {
        if (Status != BuyNowReservationStatus.PendingPayment)
            return AuctionErrors.BuyNowReservation.InvalidState(Status.Id, "attach payment");

        PaymentTransactionId = paymentTransactionId;
        ModifiedAt = nowUtc;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkPaid(DateTime nowUtc)
    {
        if (Status != BuyNowReservationStatus.PendingPayment)
            return AuctionErrors.BuyNowReservation.InvalidState(Status.Id, "mark as paid");

        Status = BuyNowReservationStatus.Paid;
        ReleasedAt = nowUtc;
        ModifiedAt = nowUtc;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> LinkOrder(OrderId orderId, DateTime nowUtc)
    {
        OrderId = orderId;
        ModifiedAt = nowUtc;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Expire(DateTime nowUtc)
    {
        if (Status != BuyNowReservationStatus.PendingPayment)
            return AuctionErrors.BuyNowReservation.InvalidState(Status.Id, "expire");

        Status = BuyNowReservationStatus.Expired;
        ReleasedAt = nowUtc;
        ModifiedAt = nowUtc;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Fail(string reason, DateTime nowUtc)
    {
        if (Status != BuyNowReservationStatus.PendingPayment)
            return AuctionErrors.BuyNowReservation.InvalidState(Status.Id, "fail");

        Status = BuyNowReservationStatus.Failed;
        FailureReason = reason;
        ReleasedAt = nowUtc;
        ModifiedAt = nowUtc;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Cancel(string reason, DateTime nowUtc)
    {
        if (Status != BuyNowReservationStatus.PendingPayment)
            return AuctionErrors.BuyNowReservation.InvalidState(Status.Id, "cancel");

        Status = BuyNowReservationStatus.Cancelled;
        FailureReason = reason;
        ReleasedAt = nowUtc;
        ModifiedAt = nowUtc;

        return UnitResult.Success<Error>();
    }
}
