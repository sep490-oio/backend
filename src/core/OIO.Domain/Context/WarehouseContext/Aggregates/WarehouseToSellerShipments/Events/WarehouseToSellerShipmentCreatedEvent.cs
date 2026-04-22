using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseToSellerShipments.Events;

/// <summary>
/// Raised when a <see cref="WarehouseToSellerShipment"/> is created by the
/// <c>CreateWarehouseToSellerShipmentOnRejectionHandler</c> in response to a
/// <c>WarehouseInspectionRejectedEvent</c>. Consumers notify the seller that a
/// rejected item is being routed back.
/// </summary>
public sealed record WarehouseToSellerShipmentCreatedEvent(
    Guid WarehouseToSellerShipmentId,
    Guid WarehouseItemId,
    Guid SellerId,
    string RejectionReason,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);
