using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.OrderContext.Aggregates.Orders.Events;

public sealed record OrderCancelledEvent(
    string OrderId,
    string BuyerId,
    string OrderNumber,
    string Reason,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);
