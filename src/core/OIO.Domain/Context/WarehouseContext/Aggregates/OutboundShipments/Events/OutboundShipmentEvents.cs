using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments.Events;

public sealed record OutboundShipmentCreatedEvent(
    string OutboundShipmentId,
    string OrderId,
    string WarehouseItemId,
    string ProviderCode,
    string ClientOrderCode,
    DateTime OccurredOn) : DomainEvent(OccurredOn);

public sealed record OutboundShipmentBookedEvent(
    string OutboundShipmentId,
    string ProviderCode,
    string ClientOrderCode,
    string CarrierTrackingNumber,
    string? ShippingLabelUrl,
    DateTime OccurredOn) : DomainEvent(OccurredOn);

public sealed record OutboundShipmentPickedUpEvent(
    string OutboundShipmentId,
    string OrderId,
    string ProviderCode,
    string CarrierTrackingNumber,
    DateTime OccurredOn) : DomainEvent(OccurredOn);

public sealed record OutboundShipmentDeliveredEvent(
    string OutboundShipmentId,
    string OrderId,
    string ProviderCode,
    DateTime DeliveredAt,
    DateTime OccurredOn) : DomainEvent(OccurredOn);

public sealed record OutboundShipmentDeliveringEvent(
    string OutboundShipmentId,
    string OrderId,
    string ProviderCode,
    DateTime OccurredOn) : DomainEvent(OccurredOn);

public sealed record OutboundShipmentFailedEvent(
    string OutboundShipmentId,
    string OrderId,
    string Reason,
    DateTime OccurredOn) : DomainEvent(OccurredOn);

public sealed record OutboundShipmentReturningEvent(
    string OutboundShipmentId,
    string OrderId,
    string Reason,
    DateTime OccurredOn) : DomainEvent(OccurredOn);

public sealed record OutboundShipmentReturnedEvent(
    string OutboundShipmentId,
    string OrderId,
    DateTime OccurredOn) : DomainEvent(OccurredOn);

public sealed record OutboundShipmentCancelledEvent(
    string OutboundShipmentId,
    string OrderId,
    string Reason,
    DateTime OccurredOn) : DomainEvent(OccurredOn);

public sealed record OutboundTrackingEventRecordedEvent(
    string OutboundShipmentId,
    string NormalizedStatus,
    string ProviderCode,
    string CarrierStatusRaw,
    DateTime OccurredOn) : DomainEvent(OccurredOn);
