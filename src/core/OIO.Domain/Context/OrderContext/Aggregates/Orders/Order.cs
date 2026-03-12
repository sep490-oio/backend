using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.OrderContext.ValueObjects;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.Aggregates.Escrows;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.ShippingContext.Aggregates;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.OrderContext.Aggregates.Orders;

public sealed class Order : AggregateRoot<OrderId>, IAuditableEntity
{
    private readonly List<Escrow> _escrows = [];
    private readonly List<OutboundShipment> _outboundShipments = [];
    public OrderNumber OrderNumber { get; private set; }
    public AuctionId AuctionId { get; private set; }
    public UserId BuyerId { get; private set; }
    public UserId SellerId { get; private set; }

    // Shipping address (snapshot — denormalized from user_addresses at creation)
    public UserAddressId? ShippingAddressId { get; private set; }
    public ShippingSnapshot Shipping { get; private set; }
    public UserAddressId? BillingAddressId { get; private set; }

    // Pricing
    public OrderPricing Pricing { get; private set; }
    public string Currency { get; private set; }

    // Status + Payment
    public OrderStatus Status { get; private set; }
    public DateTime? PaymentDueAt { get; private set; }
    public int PaymentAttemptCount { get; private set; }
    public DateTime? LastPaymentAttemptAt { get; private set; }
    public string? PaymentFailureReason { get; private set; }

    // Timestamps
    public DateTime? PaidAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    public int Version { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    // Navigation
    public OrderReturn? Return { get; private set; }  // 1:1 via uq_order_returns_order
    public IReadOnlyCollection<Escrow> Escrows => _escrows.AsReadOnly();
    public IReadOnlyCollection<OutboundShipment> OutboundShipments => _outboundShipments.AsReadOnly();

    private Order() { }
}