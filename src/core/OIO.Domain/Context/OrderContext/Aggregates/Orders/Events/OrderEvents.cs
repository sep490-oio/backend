using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.OrderContext.Aggregates.Orders.Events;

public sealed record OrderCancelledEvent(
    string OrderId,
    string BuyerId,
    string OrderNumber,
    string Reason,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record OrderCompletedEvent(
    string OrderId,
    string BuyerId,
    string SellerId,
    string OrderNumber,
    DateTime CompletedAt,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record OrderCreatedEvent(
    string OrderId,
    string BuyerId,
    string SellerId,
    string OrderNumber,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record OrderReturnOpenedByDisputeEvent(
    Guid OrderReturnId,
    Guid OrderId,
    Guid BuyerId,
    Guid SellerId,
    string FeePayer,
    DateTime BuyerDecisionDueAt,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record OrderReturnExpiredEvent(
    Guid OrderReturnId,
    Guid OrderId,
    Guid BuyerId,
    Guid SellerId,
    string Reason,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

/// <summary>
/// Raised by <c>ConfirmOrderReturnReceivedCommandHandler</c> (D1 compensating path)
/// when <c>RefundBuyerAsync</c> fails AFTER <see cref="OrderReturn.Resolve"/> has
/// committed. Observability + alerting driver — the handler also creates an admin
/// retry ticket synchronously. <c>IntentJson</c> is the serialized intent + amount
/// so downstream consumers can replay the refund without re-loading the aggregate.
/// </summary>
public sealed record DeferredRefundFailedEvent(
    Guid OrderId,
    Guid OrderReturnId,
    Guid BuyerId,
    Guid SellerId,
    string IntentJson,
    string ErrorMessage,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);
