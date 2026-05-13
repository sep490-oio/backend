using CSharpFunctionalExtensions;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.DomainEvents;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.PaymentContext.Aggregates.Escrows;

public sealed class Escrow : AggregateRoot<EscrowId>
{
    private readonly List<EscrowReleaseEvent> _releaseEvents = [];

    public OrderId OrderId { get; private set; }
    public TransactionId? HoldTransactionId { get; private set; }
    public TransactionId? ReleaseTransactionId { get; private set; }
    public Money Amount { get; private set; }
    public string Currency { get; private set; }
    public EscrowStatus Status { get; private set; }
    public DateTime HeldAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }
    public EscrowReleaseTo ReleasedTo { get; private set; }

    // Navigation
    public IReadOnlyCollection<EscrowReleaseEvent> ReleaseEvents => _releaseEvents.AsReadOnly();
    public Order Order { get; private set; }

    private Escrow() { }

    private Escrow(
        EscrowId id,
        OrderId orderId,
        TransactionId holdTransactionId,
        Money amount,
        string currency,
        DateTime now)
        : base(id)
    {
        OrderId = orderId;
        HoldTransactionId = holdTransactionId;
        Amount = amount;
        Currency = currency;
        Status = EscrowStatus.Holding;
        HeldAt = now;
        ReleasedTo = EscrowReleaseTo.None;
    }

    public static Result<Escrow, SeedWork.Errors.Error> Create(
        OrderId orderId,
        TransactionId holdTransactionId,
        Money amount,
        string currency,
        DateTime now)
    {
        var escrow = new Escrow(
            EscrowId.From(Guid.CreateVersion7()),
            orderId,
            holdTransactionId,
            amount,
            currency,
            now);

        return Result.Success<Escrow, SeedWork.Errors.Error>(escrow);
    }

    public UnitResult<SeedWork.Errors.Error> ReleaseToSeller(
        TransactionId releaseTransactionId,
        UserId createdBy,
        DateTime now)
    {
        if (Status != EscrowStatus.Holding)
        {
            return SeedWork.Errors.Error.Conflict("Escrow.InvalidStatus", "Escrow is not in holding state.");
        }

        Status = EscrowStatus.ReleasedToSeller;
        ReleasedTo = EscrowReleaseTo.Seller;
        ReleaseTransactionId = releaseTransactionId;
        ReleasedAt = now;

        var releaseEvent = EscrowReleaseEvent.Create(
            Id,
            EscrowReleaseType.Full,
            "SellerRelease",
            null,
            Amount.Amount,
            createdBy,
            now);
            
        _releaseEvents.Add(releaseEvent.Value);

        RaiseDomainEvent(new EscrowReleasedToSellerDomainEvent(
            Id, OrderId.Value, Amount.Amount, Currency, now));

        return UnitResult.Success<SeedWork.Errors.Error>();
    }

    public UnitResult<SeedWork.Errors.Error> RefundToBuyer(
        TransactionId refundTransactionId,
        UserId createdBy,
        DateTime now)
    {
        if (Status != EscrowStatus.Holding)
        {
            return SeedWork.Errors.Error.Conflict("Escrow.InvalidStatus", "Escrow is not in holding state.");
        }

        Status = EscrowStatus.RefundedToBuyer;
        ReleasedTo = EscrowReleaseTo.Buyer;
        ReleaseTransactionId = refundTransactionId;
        ReleasedAt = now;

        var releaseEvent = EscrowReleaseEvent.Create(
            Id,
            EscrowReleaseType.Refund,
            "BuyerRefund",
            null,
            Amount.Amount,
            createdBy,
            now);
            
        _releaseEvents.Add(releaseEvent.Value);

        RaiseDomainEvent(new EscrowRefundedToBuyerDomainEvent(
            Id, OrderId.Value, Amount.Amount, Currency, now));

        return UnitResult.Success<SeedWork.Errors.Error>();
    }

    public UnitResult<SeedWork.Errors.Error> ForfeitToPlatform(
        TransactionId forfeitTransactionId,
        UserId createdBy,
        DateTime now)
    {
        if (Status != EscrowStatus.Holding)
        {
            return SeedWork.Errors.Error.Conflict("Escrow.InvalidStatus", "Escrow is not in holding state.");
        }

        Status = EscrowStatus.ForfeitedToPlatform;
        ReleasedTo = EscrowReleaseTo.Platform;
        ReleaseTransactionId = forfeitTransactionId;
        ReleasedAt = now;

        var releaseEvent = EscrowReleaseEvent.Create(
            Id,
            EscrowReleaseType.Forfeit,
            "PlatformForfeit",
            null,
            Amount.Amount,
            createdBy,
            now);
            
        _releaseEvents.Add(releaseEvent.Value);

        RaiseDomainEvent(new EscrowForfeitedToPlatformDomainEvent(
            Id, OrderId.Value, Amount.Amount, Currency, now));

        return UnitResult.Success<SeedWork.Errors.Error>();
    }
}