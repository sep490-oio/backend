using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
using OIO.Domain.Context.ShippingContext.Enums;
using OIO.Domain.Context.ShippingContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ShippingContext.Aggregates;

public sealed class WarehouseItem : AggregateRoot<WarehouseItemId>, IAuditableEntity
{
    public ItemId ItemId { get; private set; }
    public InboundShipmentId InboundShipmentId { get; private set; }
    public WarehouseStorageLocationId? StorageLocationId { get; private set; }
    public WarehouseItemCondition ConditionOnArrival { get; private set; }
    public string? InspectionNotes { get; private set; }
    public string? InspectionImages { get; private set; }  // jsonb array
    public WarehouseItemStatus Status { get; private set; }
    public UserId? InspectedBy { get; private set; }
    public DateTime? InspectedAt { get; private set; }
    public DateTime? ReceivedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    // Navigation
    public InboundShipment InboundShipment { get; private set; } = null!;
    public WarehouseStorageLocation? StorageLocation { get; private set; }
    public OutboundShipment? OutboundShipment { get; private set; }  // 1:1 via outbound_shipments.warehouse_item_id
    public Item Item { get; private set; }

    private WarehouseItem() { }
}