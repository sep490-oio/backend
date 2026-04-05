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
