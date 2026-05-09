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
    public bool IsPlatformVerifiedItem { get; private set; }

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

    // Seller ship-by SLA + escalation tracking
    public DateTime? ShipByAt { get; private set; }
    public bool IsShippingOverdue { get; private set; }
    public DateTime? EscalatedAt { get; private set; }
    public string? EscalationReason { get; private set; }

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
        bool isPlatformVerifiedItem = false,
        string? notes = null)
    {
        var orderNumberValue = $"ORD-{nowUtc:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..31];
        var orderNumberResult = OrderNumber.Create(orderNumberValue);

        if (orderNumberResult.IsFailure)
            return orderNumberResult.Error;

        var order = new Order
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
            IsPlatformVerifiedItem = isPlatformVerifiedItem,
            Status = OrderStatus.PendingPayment,
            PaymentDueAt = paymentDueAt,
            PaymentAttemptCount = 0,
            Notes = notes,
            CreatedAt = nowUtc,
            ModifiedAt = nowUtc
        };

        order.RaiseDomainEvent(new OrderCreatedEvent(
            order.Id.ToString(),
            order.BuyerId.ToString(),
            order.SellerId.ToString(),
            order.OrderNumber.Value,
            nowUtc));

        return order;
    }

    /// <summary>
    /// Replaces the shipping snapshot on this order. Allowed while the order
    /// is awaiting payment OR already paid (but not yet picked up by seller
    /// fulfillment). Once the order transitions to Processing or beyond, the
    /// shipping snapshot is frozen. This supports buy-now orders whose
    /// initial snapshot was only a legacy placeholder.
    /// </summary>
    public UnitResult<Error> UpdateShipping(ShippingSnapshot newShipping, DateTime nowUtc)
    {
        if (Status != OrderStatus.PendingPayment && Status != OrderStatus.Paid)
            return Errors.OrderErrors.Order.InvalidState(Status.Id, "update shipping");

        Shipping = newShipping;
        ModifiedAt = nowUtc;
        return UnitResult.Success<Error>();
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

    /// <summary>
    /// Seller confirms a paid order and begins fulfillment. Transitions
    /// Paid → Processing. Only valid while status is Paid. Seller-side
    /// handler enforces the seller-is-caller check before calling this.
    /// </summary>
    public UnitResult<Error> ConfirmBySeller(DateTime nowUtc)
    {
        if (Status != OrderStatus.Paid)
            return Errors.OrderErrors.Order.InvalidState(Status.Id, "confirm order");

        Status = OrderStatus.Processing;
        ModifiedAt = nowUtc;
        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// LEGACY path. Retained for older call sites that still mark orders as
    /// `shipped` directly. New code must use MarkPickedUpBySeller /
    /// MarkOnDeliveringBySeller (self-ship) or the outbound-shipment event
    /// handlers (warehouse-managed) which target picked_up / on_delivering.
    /// </summary>
    public UnitResult<Error> MarkAsShipped(DateTime nowUtc)
    {
        if (Status != OrderStatus.Paid && Status != OrderStatus.Processing)
            return Errors.OrderErrors.Order.InvalidState(Status.Id, "mark as shipped");

        Status = OrderStatus.Shipped;
        ShippedAt = nowUtc;
        ModifiedAt = nowUtc;

        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Seller (self-ship) marks the order as picked up from their end.
    /// Transitions Processing → PickedUp. Also populates ShippedAt on the
    /// first fulfillment step so existing timeline UI that reads ShippedAt
    /// keeps rendering a timestamp. Caller must verify seller ownership
    /// and seller_self_ship flow before invoking.
    /// </summary>
    public UnitResult<Error> MarkPickedUp(DateTime nowUtc)
    {
        if (Status != OrderStatus.Processing)
            return Errors.OrderErrors.Order.InvalidState(Status.Id, "mark picked up");

        Status = OrderStatus.PickedUp;
        ShippedAt ??= nowUtc;
        ModifiedAt = nowUtc;
        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Seller (self-ship) marks the order as handed to delivery and in
    /// transit. Transitions PickedUp → OnDelivering. Legacy `shipped` rows
    /// are also accepted so self-ship sellers can advance historical
    /// orders into the new progression without a data repair step.
    /// </summary>
    public UnitResult<Error> MarkOnDelivering(DateTime nowUtc)
    {
        if (Status != OrderStatus.PickedUp && Status != OrderStatus.Shipped)
            return Errors.OrderErrors.Order.InvalidState(Status.Id, "mark on delivering");

        Status = OrderStatus.OnDelivering;
        ModifiedAt = nowUtc;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkAsDelivered(DateTime deliveredAt, DateTime decisionWindowEndsAt, DateTime nowUtc)
    {
        // Canonical predecessors: OnDelivering (new flow) or Shipped (legacy
        // rows). Processing is kept for back-compat with callers that skip
        // the progression for instant deliveries.
        if (Status != OrderStatus.OnDelivering &&
            Status != OrderStatus.Shipped &&
            Status != OrderStatus.PickedUp &&
            Status != OrderStatus.Processing)
            return Errors.OrderErrors.Order.InvalidState(Status.Id, "mark as delivered");

        Status = OrderStatus.Delivered;
        DeliveredAt = deliveredAt;
        DecisionWindowEndsAt = decisionWindowEndsAt;
        ModifiedAt = nowUtc;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkAsDisputed(DateTime nowUtc)
    {
        if (Status != OrderStatus.Delivered &&
            Status != OrderStatus.Paid &&
            Status != OrderStatus.Shipped &&
            Status != OrderStatus.PickedUp &&
            Status != OrderStatus.OnDelivering)
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

    /// <summary>
    /// Opens a pre-approved OrderReturn in response to dispute resolution.
    /// Unlike <see cref="RequestReturn"/> (which requires Status == Delivered), this
    /// method is callable from Disputed/Completed/etc. — dispute resolution may fire
    /// from any post-delivery status. The Dispute state-machine is the upstream guard.
    /// </summary>
    public Result<OrderReturn, Error> OpenReturnViaDispute(
        string reasonCode,
        string? description,
        ShippingFeePayer feePayer,
        DeferredRefundIntent deferredRefundIntent,
        decimal? deferredRefundAmount,
        DateTime buyerDecisionDueAt,
        DateTime nowUtc,
        string? qrToken = null)
    {
        // Refund-intent consistency: Partial requires a positive amount; Full / None must NOT carry an amount.
        if (deferredRefundIntent == DeferredRefundIntent.Partial)
        {
            if (deferredRefundAmount is null or <= 0)
                return Error.Validation(
                    "deferredRefundAmount",
                    "Order.InvalidDeferredRefund",
                    "Partial deferred-refund intent requires a positive amount.");
        }
        else
        {
            if (deferredRefundAmount is not null)
                return Error.Validation(
                    "deferredRefundAmount",
                    "Order.InvalidDeferredRefund",
                    $"Deferred-refund amount is only valid for Partial intent, not '{deferredRefundIntent.Id}'.");
        }

        // Idempotency guard (mirrors line 311): if Return exists and is non-terminal, reject.
        if (Return is not null
            && Return.Status != OrderReturnStatus.Cancelled
            && Return.Status != OrderReturnStatus.Resolved
            && Return.Status != OrderReturnStatus.Rejected)
        {
            return Errors.OrderErrors.Order.ReturnAlreadyExists(Id);
        }

        var orderReturn = OrderReturn.Create(
            Id, BuyerId, reasonCode, description, buyerDecisionDueAt, nowUtc);

        var approveResult = orderReturn.Approve(
            reason: "Pre-approved by dispute resolution",
            nowUtc: nowUtc,
            qrToken: qrToken);
        if (approveResult.IsFailure)
            return approveResult.Error;

        orderReturn.ShippingFeePayer     = feePayer;              // internal set — same namespace access
        orderReturn.DeferredRefundIntent = deferredRefundIntent;  // internal set — same namespace access
        orderReturn.DeferredRefundAmount = deferredRefundAmount;

        Return     = orderReturn;
        ModifiedAt = nowUtc;

        RaiseDomainEvent(new OrderReturnOpenedByDisputeEvent(
            OrderReturnId: orderReturn.Id.Value,
            OrderId: Id.Value,
            BuyerId: BuyerId.Value,
            SellerId: SellerId.Value,
            FeePayer: feePayer.Id,
            BuyerDecisionDueAt: buyerDecisionDueAt,
            OccurredAt: nowUtc));

        return orderReturn;
    }

    /// <summary>
    /// Raises <see cref="OrderReturnExpiredEvent"/> for an already-cancelled
    /// <see cref="OrderReturn"/>. Called by <c>OrderReturnDeadlineWatcherJob</c>
    /// from the owning <see cref="Order"/> aggregate so the event ends up in the
    /// transactional outbox with the rest of the order's state.
    /// </summary>
    public void RaiseOrderReturnExpired(OrderReturn orderReturn, DateTime nowUtc)
    {
        RaiseDomainEvent(new OrderReturnExpiredEvent(
            OrderReturnId: orderReturn.Id.Value,
            OrderId:       Id.Value,
            BuyerId:       BuyerId.Value,
            SellerId:      SellerId.Value,
            Reason:        orderReturn.DecisionReason ?? "Deadline expired",
            OccurredAt:    nowUtc));

        ModifiedAt = nowUtc;
    }

    /// <summary>
    /// Raises <see cref="DeferredRefundFailedEvent"/> for the owning <see cref="Return"/>.
    /// Called by <c>ConfirmOrderReturnReceivedCommandHandler</c>'s D1 compensating
    /// path after <c>RefundBuyerAsync</c> fails post-<see cref="OrderReturn.Resolve"/>.
    /// Must be invoked via the aggregate root so the event lands in the same outbox
    /// write as the admin retry ticket.
    /// </summary>
    public void RaiseDeferredRefundFailed(
        OrderReturn orderReturn,
        string intentJson,
        string errorMessage,
        DateTime nowUtc)
    {
        RaiseDomainEvent(new DeferredRefundFailedEvent(
            OrderId:       Id.Value,
            OrderReturnId: orderReturn.Id.Value,
            BuyerId:       BuyerId.Value,
            SellerId:      SellerId.Value,
            IntentJson:    intentJson,
            ErrorMessage:  errorMessage,
            OccurredAt:    nowUtc));

        ModifiedAt = nowUtc;
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

    /// <summary>
    /// Stamps the seller ship-by SLA deadline onto the order. Idempotent:
    /// once set, subsequent calls are a no-op so retries / replays do not
    /// clobber an earlier deadline.
    /// </summary>
    public UnitResult<Error> StampShipBySla(DateTime shipByAt)
    {
        if (ShipByAt is not null)
            return UnitResult.Success<Error>();

        ShipByAt = shipByAt;
        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Marks the order as overdue on the seller ship-by SLA and records the
    /// escalation metadata. Idempotent: subsequent calls return success with
    /// value=false so the overdue-scan job can safely reprocess the same row
    /// without re-emitting alerts/notifications. Returns true only on the
    /// transition from not-overdue → overdue.
    /// </summary>
    public Result<bool, Error> MarkShippingOverdue(string reason, DateTime nowUtc)
    {
        if (IsShippingOverdue)
            return false;

        IsShippingOverdue = true;
        EscalatedAt = nowUtc;
        EscalationReason = reason;
        ModifiedAt = nowUtc;
        return true;
    }

    // ────────────────────────────────────────────────────────────────────
    // Admin-only interventions — bypass normal state-machine guards.
    // Authorization is enforced at the endpoint level (admin:payments:manage).
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Admin force-cancel: allows cancellation from any non-terminal status.
    /// Financial consequences (escrow release, deposit refund) are handled
    /// by the command handler, not in this domain method.
    /// </summary>
    public UnitResult<Error> AdminForceCancel(string reason, DateTime nowUtc)
    {
        if (Status == OrderStatus.Cancelled || Status == OrderStatus.Refunded)
            return Errors.OrderErrors.Order.InvalidState(Status.Id, "admin force cancel");

        Status = OrderStatus.Cancelled;
        CancelledAt = nowUtc;
        Notes = AppendNote(Notes, $"[ADMIN] Force Cancel: {reason}");
        ModifiedAt = nowUtc;

        RaiseDomainEvent(new OrderCancelledEvent(
            OrderId: $"{Id}",
            BuyerId: $"{BuyerId}",
            OrderNumber: OrderNumber.Value,
            Reason: $"[ADMIN] {reason}",
            OccurredAt: nowUtc));

        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Admin force-refund: marks order as refunded from any non-terminal status.
    /// Actual wallet refund + escrow release is handled by the command handler.
    /// </summary>
    public UnitResult<Error> AdminForceRefund(string reason, DateTime nowUtc)
    {
        if (Status == OrderStatus.Refunded || Status == OrderStatus.Cancelled)
            return Errors.OrderErrors.Order.InvalidState(Status.Id, "admin force refund");

        Status = OrderStatus.Refunded;
        Notes = AppendNote(Notes, $"[ADMIN] Force Refund: {reason}");
        ModifiedAt = nowUtc;

        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Admin override status: direct status transition without normal guards.
    /// Use with extreme caution — this can put the order into an inconsistent
    /// state. All overrides are logged in Notes for audit trail.
    /// </summary>
    public UnitResult<Error> AdminOverrideStatus(OrderStatus newStatus, string reason, DateTime nowUtc)
    {
        var oldStatus = Status;
        Status = newStatus;
        Notes = AppendNote(Notes, $"[ADMIN] Status override {oldStatus.Id} → {newStatus.Id}: {reason}");
        ModifiedAt = nowUtc;
        return UnitResult.Success<Error>();
    }

    private static string? AppendNote(string? existing, string note) =>
        string.IsNullOrEmpty(existing) ? note : $"{existing}\n{note}";

    private Order() { }
}
