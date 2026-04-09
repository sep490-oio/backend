namespace OIO.Application.Context.OrderContext.DTOs;

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    Guid AuctionId,
    Guid BuyerId,
    Guid SellerId,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt,
    DateTime? PaymentDueAt,
    DateTime? PaidAt,
    DateTime? ShippedAt,
    DateTime? DeliveredAt,
    DateTime? DecisionWindowEndsAt,
    DateTime? CompletedAt,
    DateTime? CancelledAt,
    string? EscrowStatus,
    string? TrackingNumber,
    OrderReturnDto? Return,
    string? BuyerDisplayName = null,
    string? SellerDisplayName = null,
    OrderShippingDto? Shipping = null,
    OrderItemSummaryDto? Item = null,
    SellerFulfillmentDto? SellerFulfillment = null,
    /// <summary>
    /// Viewer-scoped: true when the current caller is the order's buyer and
    /// the order is still in pending_payment or paid (before seller begins
    /// fulfillment). FE uses this to decide whether to render the editable
    /// shipping form on the order detail page.
    /// </summary>
    bool BuyerCanUpdateShipping = false,
    /// <summary>Sum of escrow amounts for this order (holding or released). Null when no escrows exist.</summary>
    decimal? AmountPaid = null,
    /// <summary>Portion applied from an auction buy-now deposit. Null when not computed.</summary>
    decimal? DepositAppliedAmount = null,
    /// <summary>Portion committed from the buyer's wallet (hybrid hold). Null when not computed.</summary>
    decimal? WalletAppliedAmount = null,
    /// <summary>Portion paid through the payment gateway (e.g. VnPay). Null when not computed.</summary>
    decimal? GatewayPaidAmount = null,
    /// <summary>
    /// Seller direct-ship shipment (1:1 with the order). Null for orders that
    /// don't use the self-ship-direct fulfillment flow, or when the shipment
    /// has not been created yet.
    /// </summary>
    SellerDirectShipmentDto? DirectShipment = null,
    /// <summary>
    /// Warehouse outbound shipment snapshot (1:1 with the active outbound for
    /// warehouse_managed orders). Null for orders that don't use the warehouse
    /// outbound flow or when viewed by a non-buyer.
    /// </summary>
    OrderWarehouseOutboundShipmentDto? WarehouseOutboundShipment = null);

/// <summary>
/// Compact warehouse outbound shipment snapshot exposed on the buyer-scoped
/// OrderDto so the order detail page can render the action hub panel (view,
/// acknowledge received, accept, dispute) without a second round-trip.
/// </summary>
public sealed record OrderWarehouseOutboundShipmentDto(
    Guid ShipmentId,
    string Status,
    string ShipmentMode,
    string ProviderCode,
    string? ExternalCarrierName,
    string? CarrierTrackingNumber,
    string? ClientOrderCode,
    string? QrPayload,
    bool QrAvailable,
    DateTime? DispatchedAt,
    DateTime? DeliveredAt,
    DateTime? BuyerReceivedPackageAt,
    DateTime? BuyerAcceptedAt,
    bool CanAcknowledgeReceived,
    bool CanAccept,
    bool CanOpenDispute,
    bool HasActiveDispute,
    DateTime? DecisionWindowEndsAt,
    bool CanSubmitProof,
    bool HasBuyerReceiptProof = false,
    bool CanSubmitReceiptProof = false);

/// <summary>
/// Compact item summary attached to every OrderDto so Checkout / MyOrders /
/// OrderDetail can render product context (image, title, price) without
/// making a second query.
///
/// <see cref="FinalPrice"/> is the hammer/sale price locked on the order
/// (order.Pricing.ItemPrice.Amount). <see cref="StartingPrice"/> is the
/// auction's original starting price.
/// <see cref="PrimaryImageUrl"/> is null when no primary image exists —
/// FE falls back to a placeholder.
/// </summary>
public sealed record OrderItemSummaryDto(
    Guid ItemId,
    Guid AuctionId,
    string ItemTitle,
    string? PrimaryImageUrl,
    decimal StartingPrice,
    decimal FinalPrice,
    string Currency);

/// <summary>
/// Seller-side fulfillment metadata attached to seller-scoped order queries.
/// Tells the seller UI which shipment path applies for this order:
///   - "book_outbound" → item sits in a warehouse; seller books an outbound shipment via BookOutboundShipmentCommand
///   - "self_ship"     → seller ships directly via SelfShipOrderCommand
///
/// <see cref="HasActiveOutboundShipment"/> is true when at least one non-terminal
/// OutboundShipment already exists for the order; in that case the UI should
/// offer "View Shipment" instead of "Create Shipment".
/// </summary>
/// <summary>
/// Seller fulfillment metadata with ownership-aware semantics.
///
/// <see cref="FulfillmentFlow"/> values:
///   - <c>seller_self_ship</c>: seller ships directly to the buyer. No OutboundShipment
///     is created via the warehouse flow; the seller confirms the shipment via the
///     order-level self-ship endpoint.
///   - <c>warehouse_managed</c>: goods are held at the platform warehouse. Only
///     warehouse staff may book the outbound shipment via the warehouse API.
///
/// <see cref="SellerCanCreateShipment"/>: true only when the seller is the owner of the
/// shipping action (i.e. <c>seller_self_ship</c> flow). False for warehouse_managed.
///
/// <see cref="WarehouseStaffMustBookOutbound"/>: true when the order is warehouse-managed
/// and still awaiting outbound booking; FE surfaces an "Awaiting warehouse outbound"
/// status instead of a create-shipment CTA.
///
/// <see cref="FulfillmentMode"/> is retained for backward-compat (same semantics as
/// <see cref="FulfillmentFlow"/>) so in-flight FE code keeps working during the rollout.
/// </summary>
public sealed record SellerFulfillmentDto(
    string FulfillmentMode,
    string FulfillmentFlow,
    bool SellerCanCreateShipment,
    bool WarehouseStaffMustBookOutbound,
    Guid? WarehouseItemId,
    bool HasActiveOutboundShipment,
    Guid? OutboundShipmentId,
    SellerFulfillmentPackageDefaultsDto? PackageDefaults,
    /// <summary>
    /// Ship-by SLA deadline for seller_self_ship flow. Null for warehouse_managed
    /// or when the order has not yet reached Paid. Stamped once on Paid by
    /// <c>SellerSelfShipSlaScheduler</c>.
    /// </summary>
    DateTime? ShipByAt = null,
    /// <summary>True after the overdue scan job flags this order past its ShipByAt.</summary>
    bool IsShippingOverdue = false,
    /// <summary>Timestamp the overdue scan raised the escalation.</summary>
    DateTime? EscalatedAt = null,
    /// <summary>Short code describing why the order was escalated (e.g. seller_ship_sla_missed).</summary>
    string? EscalationReason = null);

public sealed record SellerFulfillmentPackageDefaultsDto(
    int? WeightGrams,
    int? LengthCm,
    int? WidthCm,
    int? HeightCm,
    decimal? InsuranceValue);

/// <summary>
/// Shipping snapshot exposed on OrderDto. Mirrors the address-book shape so
/// the FE can prefill the checkout form directly from the server response.
/// Null entries indicate a legacy/placeholder snapshot that must be re-entered
/// before the buyer can pay.
/// </summary>
public sealed record OrderShippingDto(
    string? RecipientName,
    string? PhoneNumber,
    string? Street,
    string? Ward,
    string? District,
    string? City,
    string? PostalCode,
    string ComposedAddress,
    bool IsStructured);
