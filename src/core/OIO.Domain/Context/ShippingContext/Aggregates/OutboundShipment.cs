using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.ShippingContext.Enums;
using OIO.Domain.Context.ShippingContext.ValueObjects;
using OIO.Domain.Context.ShippingContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ShippingContext.Aggregates;

public sealed class OutboundShipment : AggregateRoot<OutboundShipmentId>, IAuditableEntity
{
    public OrderId OrderId { get; private set; }
    public WarehouseItemId WarehouseItemId { get; private set; }
    public string ProviderCode { get; private set; }
    public string ClientOrderCode { get; private set; }
    public string? CarrierTrackingNumber { get; private set; }
    public string? ShippingLabelUrl { get; private set; }
    public string? ShippingMethod { get; private set; }
    public string? RecipientCarrierAddressData { get; private set; }  // jsonb

    // Package dimensions
    public PackageDimensions Package { get; private set; }
    public ShippingCost Cost { get; private set; }
    public short? PaymentTypeId { get; private set; }
    public string? HandlingNote { get; private set; }
    public string? ExtraData { get; private set; }  // jsonb

    public OutboundShipmentStatus Status { get; private set; }
    public UserId? PackedBy { get; private set; }
    public DateTime? PackedAt { get; private set; }
    public DateTime? DispatchedAt { get; private set; }
    public DateTime? EstimatedDeliveryAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    // Navigation
    public WarehouseItem WarehouseItem { get; private set; } = null!;
    public Order Order { get; private set; }

    private OutboundShipment() { }
}