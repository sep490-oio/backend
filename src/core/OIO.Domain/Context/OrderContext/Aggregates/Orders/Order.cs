using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders.Events;
using OIO.Domain.Context.OrderContext.ValueObjects;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.Aggregates.Escrows;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

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
    public DateTime? DecisionWindowEndsAt { get; private set; }
    public DateTime? DisputedAt { get; private set; }
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

    public static Result<Order, Error> Create(
        AuctionId auctionId,
        UserId buyerId,
        UserId sellerId,
        ShippingSnapshot shipping,
        UserAddressId? shippingAddressId,
        UserAddressId? billingAddressId,
        OrderPricing pricing,
        string currency,
        DateTime paymentDueAt,
        DateTime nowUtc,
        string? notes = null)
    {
        var orderNumberValue = $"ORD-{nowUtc:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..31];
        var orderNumberResult = OrderNumber.Create(orderNumberValue);

        if (orderNumberResult.IsFailure)
            return orderNumberResult.Error;

        return new Order
        {
            Id = OrderId.From(Guid.CreateVersion7()),
            OrderNumber = orderNumberResult.Value,
            AuctionId = auctionId,
            BuyerId = buyerId,
            SellerId = sellerId,
            ShippingAddressId = shippingAddressId,
            Shipping = shipping,
            BillingAddressId = billingAddressId,
            Pricing = pricing,
            Currency = currency,
            Status = OrderStatus.PendingPayment,
            PaymentDueAt = paymentDueAt,
            PaymentAttemptCount = 0,
            Notes = notes,
            CreatedAt = nowUtc,
            ModifiedAt = nowUtc
        };
    }

    public UnitResult<Error> InitializePayment(DateTime nowUtc)
    {
        if (Status != OrderStatus.PendingPayment)
            return Errors.OrderErrors.Order.CannotInitializePayment(Status);

        PaymentAttemptCount++;
        LastPaymentAttemptAt = nowUtc;
        ModifiedAt = nowUtc;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkAsPaid(DateTime nowUtc)
    {
        if (Status != OrderStatus.PendingPayment)
            return Errors.OrderErrors.Order.InvalidState(Status.Id, "mark as paid");

        Status = OrderStatus.Paid;
        PaidAt = nowUtc;
        ModifiedAt = nowUtc;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkAsShipped(DateTime nowUtc)
    {
        if (Status != OrderStatus.Paid && Status != OrderStatus.Processing)
            return Errors.OrderErrors.Order.InvalidState(Status.Id, "mark as shipped");

        Status = OrderStatus.Shipped;
        ShippedAt = nowUtc;
        ModifiedAt = nowUtc;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkAsDelivered(DateTime deliveredAt, DateTime decisionWindowEndsAt, DateTime nowUtc)
    {
        if (Status != OrderStatus.Shipped && Status != OrderStatus.Processing)
            return Errors.OrderErrors.Order.InvalidState(Status.Id, "mark as delivered");

        Status = OrderStatus.Delivered;
        DeliveredAt = deliveredAt;
        DecisionWindowEndsAt = decisionWindowEndsAt;
        ModifiedAt = nowUtc;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkAsDisputed(DateTime nowUtc)
    {
        if (Status != OrderStatus.Delivered && Status != OrderStatus.Paid && Status != OrderStatus.Shipped)
            return Errors.OrderErrors.Order.InvalidState(Status.Id, "mark as disputed");

        Status = OrderStatus.Disputed;
        DisputedAt = nowUtc;
        ModifiedAt = nowUtc;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Complete(DateTime nowUtc)
    {
        if (Status != OrderStatus.Delivered && Status != OrderStatus.Processing && Status != OrderStatus.Paid)
            return Errors.OrderErrors.Order.InvalidState(Status.Id, "complete");

        Status = OrderStatus.Completed;
        CompletedAt = nowUtc;
        ModifiedAt = nowUtc;

        RaiseDomainEvent(new OrderCompletedEvent(
            OrderId: $"{Id.Value}",
            BuyerId: $"{BuyerId.Value}",
            SellerId: $"{SellerId.Value}",
            OrderNumber: OrderNumber.Value,
            CompletedAt: nowUtc,
            OccurredAt: nowUtc));

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkAsRefunded(DateTime nowUtc)
    {
        if (Status != OrderStatus.Delivered &&
            Status != OrderStatus.Disputed &&
            Status != OrderStatus.Completed &&
            Status != OrderStatus.Paid)
        {
            return Errors.OrderErrors.Order.InvalidState(Status.Id, "mark as refunded");
        }

        Status = OrderStatus.Refunded;
        ModifiedAt = nowUtc;

        return UnitResult.Success<Error>();
    }

    public Result<OrderReturn, Error> RequestReturn(
        UserId buyerId,
        string reasonCode,
        string? description,
        DateTime nowUtc)
    {
        if (BuyerId != buyerId)
            return Error.Forbidden("Order.ReturnForbidden", "Only the buyer can request a return for this order.");

        if (Status != OrderStatus.Delivered)
            return Errors.OrderErrors.Order.InvalidState(Status.Id, "request return");

        if (DecisionWindowEndsAt is null)
            return Errors.OrderErrors.Order.DecisionWindowNotStarted(Id);

        if (nowUtc > DecisionWindowEndsAt.Value)
            return Errors.OrderErrors.Order.DecisionWindowExpired(Id);

        if (Return is not null && Return.Status != OrderReturnStatus.Cancelled && Return.Status != OrderReturnStatus.Resolved && Return.Status != OrderReturnStatus.Rejected)
            return Errors.OrderErrors.Order.ReturnAlreadyExists(Id);

        var orderReturn = OrderReturn.Create(Id, buyerId, reasonCode, description, DecisionWindowEndsAt.Value, nowUtc);
        Return = orderReturn;
        ModifiedAt = nowUtc;

        return orderReturn;
    }

    public UnitResult<Error> MarkAsPaymentFailed(string reason, DateTime nowUtc)
    {
        if (Status != OrderStatus.PendingPayment)
            return Errors.OrderErrors.Order.InvalidState(Status.Id, "mark as payment failed");

        PaymentFailureReason = reason;
        ModifiedAt = nowUtc;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Cancel(string reason, DateTime nowUtc)
    {
        if (Status != OrderStatus.PendingPayment)
            return Errors.OrderErrors.Order.InvalidState(Status.Id, "cancel");

        Status = OrderStatus.Cancelled;
        CancelledAt = nowUtc;
        Notes = !string.IsNullOrEmpty(Notes) ? $"{Notes}\nCancel Reason: {reason}" : $"Cancel Reason: {reason}";
        ModifiedAt = nowUtc;

        RaiseDomainEvent(new OrderCancelledEvent(
            OrderId: $"{Id}",
            BuyerId: $"{BuyerId}",
            OrderNumber: OrderNumber.Value,
            Reason: reason,
            OccurredAt: nowUtc));

        return UnitResult.Success<Error>();
    }

    private Order() { }
}
