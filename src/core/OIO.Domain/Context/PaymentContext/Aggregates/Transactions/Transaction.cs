using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.Aggregates.PaymentMethods;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Descriptions;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.DomainEvents;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.PaymentContext.Aggregates.Transactions;

public sealed class Transaction : AggregateRoot<TransactionId>, ICreatedAtEntity
{
    private readonly List<AuctionDeposit> _auctionDeposits = [];
    private readonly List<Wallet> _wallets;
    
    public TransactionNumber TransactionNumber { get; private set; }
    public OrderId? OrderId { get; private set; }
    public AuctionId? AuctionId { get; private set; }
    public AuctionBuyNowReservationId? BuyNowReservationId { get; private set; }
    public UserId UserId { get; private set; }
    public PaymentMethodId? PaymentMethodId { get; private set; }
    public TransactionType Type { get; private set; }
    public Money Amount { get; private set; }
    public decimal Fee { get; private set; }
    public Money NetAmount { get; private set; }
    public string Currency { get; private set; }
    public TransactionStatus Status { get; private set; }
    public GatewayInfo Gateway { get; private set; }
    public string? Description { get; private set; }
    public string? ClientReturnPath { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public PaymentMethod? PaymentMethod { get; private set; }
    public IReadOnlyCollection<AuctionDeposit> AuctionDeposits => _auctionDeposits.AsReadOnly();
    public IReadOnlyCollection<Wallet> Wallets => _wallets.AsReadOnly();

    private Transaction() { }

    /// <summary>
    /// Tạo Transaction mới với status = Pending.
    /// </summary>
    public static Result<Transaction, Error> Create(
        UserId userId,
        TransactionNumber transactionNumber,
        TransactionType type,
        Money amount,
        string currency,
        string? description,
        DateTime nowUtc,
        OrderId? orderId = null,
        AuctionId? auctionId = null,
        AuctionBuyNowReservationId? buyNowReservationId = null)
    {
        var transaction = new Transaction
        {
            Id = TransactionId.From(Guid.CreateVersion7()),
            UserId = userId,
            TransactionNumber = transactionNumber,
            Type = type,
            Amount = amount,
            Fee = 0,
            NetAmount = amount,
            Currency = currency,
            Status = TransactionStatus.Pending,
            Gateway = GatewayInfo.Empty,
            Description = description,
            CreatedAt = nowUtc,
            OrderId = orderId,
            AuctionId = auctionId,
            BuyNowReservationId = buyNowReservationId,
        };

        return Result.Success<Transaction, Error>(transaction);
    }

    /// <summary>
    /// Đánh dấu giao dịch đang xử lý (đã gửi sang VNPay).
    /// </summary>
    public UnitResult<Error> MarkAsProcessing()
    {
        if (Status != TransactionStatus.Pending)
            return Error.Conflict("Transaction.InvalidStatus",
                $"Cannot mark as processing. Current status: {Status}");

        Status = TransactionStatus.Processing;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> SetFee(decimal fee, Money netAmount)
    {
        if (Status != TransactionStatus.Pending)
            return Error.Conflict("Transaction.InvalidStatus",
                $"Cannot set fee. Current status: {Status}");

        if (fee < 0)
            return Error.Validation("Fee", "Transaction.InvalidFee", "Fee must be non-negative.");

        Amount.EnsureSameCurrency(netAmount);

        if (netAmount.Amount < 0 || netAmount.Amount + fee > Amount.Amount)
            return Error.Validation("NetAmount", "Transaction.InvalidNetAmount",
                "Net amount plus fee must be less than or equal to the gross amount.");

        Fee = fee;
        NetAmount = netAmount;
        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Thanh toán thành công — cập nhật gateway info và status.
    /// </summary>
    public UnitResult<Error> MarkAsCompleted(GatewayInfo gatewayInfo, DateTime processedAt)
    {
        if (Status != TransactionStatus.Pending && Status != TransactionStatus.Processing)
            return Error.Conflict("Transaction.InvalidStatus",
                $"Cannot mark as completed. Current status: {Status}");

        Status = TransactionStatus.Completed;
        Gateway = gatewayInfo;
        ProcessedAt = processedAt;

        RaiseDomainEvent(new TransactionCompletedDomainEvent(
            Id, UserId, Amount.Amount, Currency, Type.Id, processedAt));

        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Thanh toán thất bại.
    /// </summary>
    public UnitResult<Error> MarkAsFailed(GatewayInfo gatewayInfo, DateTime processedAt)
    {
        if (Status != TransactionStatus.Pending && Status != TransactionStatus.Processing)
            return Error.Conflict("Transaction.InvalidStatus",
                $"Cannot mark as failed. Current status: {Status}");

        Status = TransactionStatus.Failed;
        Gateway = gatewayInfo;
        ProcessedAt = processedAt;

        RaiseDomainEvent(new TransactionFailedDomainEvent(
            Id, UserId, Amount.Amount, Currency, Type.Id, processedAt));

        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Cancels a pending transaction. Used when the user retries a VNPay
    /// payment attempt for an order — the stale pending transaction is
    /// closed out so a fresh transaction (with a new txnRef) can be created
    /// for the new gateway session. Only valid while status is Pending.
    /// </summary>
    /// <param name="reason">Machine-readable reason such as "retry_payment_replaced".</param>
    /// <param name="nowUtc">Processed-at timestamp.</param>
    public UnitResult<Error> CancelPending(string reason, DateTime nowUtc)
    {
        if (Status != TransactionStatus.Pending)
            return Error.Conflict("Transaction.InvalidStatus",
                $"Cannot cancel. Only pending transactions may be cancelled. Current status: {Status}");

        Status = TransactionStatus.Cancelled;
        ProcessedAt = nowUtc;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            Description = string.IsNullOrWhiteSpace(Description)
                ? LedgerDescriptions.CancelledSuffix(reason)
                : $"{Description} {LedgerDescriptions.CancelledSuffix(reason)}";
        }
        return UnitResult.Success<Error>();
    }

    public void SetClientReturnPath(string? path)
    {
        if (path is not null && IsValidClientReturnPath(path))
            ClientReturnPath = path;
    }

    private static bool IsValidClientReturnPath(string path)
    {
        return path.StartsWith('/') &&
               !path.StartsWith("//") &&
               !path.Contains("://") &&
               !path.StartsWith("/payments/vnpay/return", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Liên kết transaction với payment method đã dùng.
    /// </summary>
    public void AssociatePaymentMethod(PaymentMethodId paymentMethodId)
    {
        PaymentMethodId = paymentMethodId;
    }

    /// <summary>
    /// Đánh dấu đã hoàn tiền.
    /// </summary>
    public UnitResult<Error> MarkAsRefunded(DateTime processedAt)
    {
        if (Status != TransactionStatus.Completed)
            return Error.Conflict("Transaction.InvalidStatus",
                $"Cannot refund. Current status: {Status}");

        Status = TransactionStatus.Refunded;
        ProcessedAt = processedAt;

        RaiseDomainEvent(new TransactionRefundedDomainEvent(
            Id, UserId, Amount.Amount, Currency, processedAt));

        return UnitResult.Success<Error>();
    }
}
