using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
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
}