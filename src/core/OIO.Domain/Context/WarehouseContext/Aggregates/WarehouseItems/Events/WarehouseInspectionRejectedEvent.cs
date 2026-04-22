using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems.Events;

/// <summary>
/// Raised when a warehouse inspector rejects a newly-arrived WarehouseItem via
/// <see cref="WarehouseInspection.Reject"/>. Handlers create a
/// <see cref="Aggregates.WarehouseToSellerShipments.WarehouseToSellerShipment"/>
/// routing the item back to its seller.
/// </summary>
public sealed record WarehouseInspectionRejectedEvent(
    Guid WarehouseInspectionId,
    Guid WarehouseItemId,
    Guid RejectedBy,
    string Reason,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);
