using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
using OIO.Domain.Context.ShippingContext.Enums;
using OIO.Domain.Context.ShippingContext.ValueObjects;
using OIO.Domain.Context.ShippingContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ShippingContext.Aggregates;

public sealed class InboundShipment : AggregateRoot<InboundShipmentId>, IAuditableEntity
{
    public ItemId ItemId { get; private set; }
    public UserId SellerId { get; private set; }
    public AuctionId AuctionId { get; private set; }
    public string ProviderCode { get; private set; }
    public string ClientOrderCode { get; private set; }
    public string? CarrierTrackingNumber { get; private set; }

    // Sender address
    public SenderAddress Sender { get; private set; }

    // Package dimensions
    public PackageDimensions Package { get; private set; }
    public ShippingCost Cost { get; private set; }
    public string? ExtraData { get; private set; }  // jsonb

    public InboundShipmentStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? ExpectedArrivalAt { get; private set; }
    public DateTime? ArrivedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    // Navigation
    public WarehouseItem? WarehouseItem { get; private set; }  // 1:1 via warehouse_items.inbound_shipment_id

    private InboundShipment() { }
}