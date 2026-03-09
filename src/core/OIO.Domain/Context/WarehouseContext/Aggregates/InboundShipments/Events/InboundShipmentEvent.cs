using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments.Events;

public sealed record InboundShipmentCreatedEvent(
    string InboundShipmentId,
    string ItemId,
    string SellerId,
    string ProviderCode,
    string ClientOrderCode,
    DateTime OccurredOn) : DomainEvent(OccurredOn);

public sealed record InboundShipmentBookedEvent(
    string InboundShipmentId,
    string ProviderCode,
    string ClientOrderCode,
    string CarrierTrackingNumber,
    DateTime OccurredOn) : DomainEvent(OccurredOn);

public sealed record InboundShipmentArrivedEvent(
    string InboundShipmentId,
    string ProviderCode,
    string CarrierTrackingNumber,
    DateTime OccurredOn) : DomainEvent(OccurredOn);

public sealed record InboundShipmentInspectedEvent(
    string InboundShipmentId,
    string ItemId,
    string ConditionOnArrival,
    string InspectedBy,
    DateTime OccurredOn) : DomainEvent(OccurredOn);

public sealed record InboundShipmentCompletedEvent(
    string InboundShipmentId,
    string ItemId,
    DateTime OccurredOn) : DomainEvent(OccurredOn);

public sealed record InboundShipmentCancelledEvent(
    string InboundShipmentId,
    string Reason,
    DateTime OccurredOn) : DomainEvent(OccurredOn);

public sealed record InboundShipmentFailedEvent(
    string InboundShipmentId,
    string Reason,
    DateTime OccurredOn) : DomainEvent(OccurredOn);

public sealed record InboundTrackingEventRecordedEvent(
    string InboundShipmentId,
    string NormalizedStatus,
    string ProviderCode,
    string CarrierStatusRaw,
    DateTime OccurredOn) : DomainEvent(OccurredOn);