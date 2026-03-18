using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems.Events;

public sealed record WarehouseItemCreatedEvent(
    string WarehouseItemId,
    string ItemId,
    string InboundShipmentId,
    DateTime OccurredOn) : DomainEvent(OccurredOn);

public sealed record WarehouseItemStoredEvent(
    string WarehouseItemId,
    string StorageLocationId,
    string LocationLabel,
    DateTime OccurredOn) : DomainEvent(OccurredOn);

public sealed record WarehouseItemReservedEvent(
    string WarehouseItemId,
    string OutboundShipmentId,
    DateTime OccurredOn) : DomainEvent(OccurredOn);

public sealed record WarehouseItemDispatchedEvent(
    string WarehouseItemId,
    string OutboundShipmentId,
    DateTime OccurredOn) : DomainEvent(OccurredOn);
