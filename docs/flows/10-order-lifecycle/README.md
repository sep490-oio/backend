# 10 -- Order Lifecycle

## 1. Order State Machine

```mermaid
stateDiagram-v2
    [*] --> PendingPayment : Order.Create()
    PendingPayment --> Paid : MarkAsPaid()
    PendingPayment --> Cancelled : Cancel()

    Paid --> Shipped : MarkAsShipped()
    Paid --> Completed : Complete()
    Paid --> Disputed : MarkAsDisputed()
    Paid --> Refunded : MarkAsRefunded()

    Processing --> Shipped : MarkAsShipped()
    Processing --> Delivered : MarkAsDelivered()
    Processing --> Completed : Complete()

    Shipped --> Delivered : MarkAsDelivered()
    Shipped --> Disputed : MarkAsDisputed()

    Delivered --> Completed : Complete()
    Delivered --> Disputed : MarkAsDisputed()
    Delivered --> Refunded : MarkAsRefunded()

    Disputed --> Refunded : MarkAsRefunded()

    Completed --> Refunded : MarkAsRefunded()

    Cancelled --> [*]
    Completed --> [*]
    Refunded --> [*]
```

## 2. Order Return State Machine

```mermaid
stateDiagram-v2
    [*] --> Requested : OrderReturn.Create()
    Requested --> Approved : Approve()
    Requested --> Rejected : Reject()

    Approved --> ReturnInTransit : MarkReturnShipped()
    Approved --> SellerReceived : MarkSellerReceived()

    ReturnInTransit --> SellerReceived : MarkSellerReceived()

    SellerReceived --> Resolved : Resolve()

    Rejected --> Resolved : Resolve()

    Requested --> Cancelled : Cancel()
    Approved --> Cancelled : Cancel()
    ReturnInTransit --> Cancelled : Cancel()
    SellerReceived --> Cancelled : Cancel()

    Resolved --> [*]
    Cancelled --> [*]
```

## 3. Path A -- Buy Now Order (auto-created, auto-paid)

```mermaid
sequenceDiagram
    participant Buyer
    participant VNPay
    participant ProcessVnPayCallback as ProcessVnPayCallbackCommand
    participant Auction
    participant Order
    participant Escrow
    participant Warehouse as OutboundShipment
    participant GHN
    participant DecisionJob as ReleaseExpiredDecisionWindowJob
    participant Settlement as EscrowSettlementService

    Buyer->>VNPay: Pay buy-now price
    VNPay->>ProcessVnPayCallback: IPN callback (purpose=AuctionBuyNow)
    ProcessVnPayCallback->>Auction: FinalizeBuyNowReservation()
    ProcessVnPayCallback->>Order: CreateBuyNowOrder() (PendingPayment)
    Note over Order: fees=0, address from buyer default
    ProcessVnPayCallback->>Escrow: Create escrow (VNPay amount)
    alt Deposit applied > 0
        ProcessVnPayCallback->>Escrow: Create escrow (deposit amount)
    end
    ProcessVnPayCallback->>Order: MarkAsPaid(now)
    Note over Order: Status = Paid, auto-paid

    Warehouse->>GHN: Book outbound shipment
    GHN-->>Warehouse: Carrier picks up
    Warehouse->>Order: OutboundShipmentPickedUpEvent -> MarkAsShipped()
    GHN-->>Warehouse: Delivery confirmed
    Warehouse->>Order: OutboundShipmentDeliveredEvent -> MarkAsDelivered()
    Note over Order: DecisionWindowEndsAt = deliveredAt + 7 days

    DecisionJob->>Settlement: ReleaseToSellerAsync()
    Settlement->>Escrow: ReleaseToSeller() for each holding escrow
    Settlement->>Order: Complete()
    Note over Order: Status = Completed
```

## 4. Path B -- Auction Winner Order (checkout required)

```mermaid
sequenceDiagram
    participant Auction
    participant AuctionSoldHandler as AuctionSoldEventHandler
    participant Order
    participant Winner as Buyer/Winner
    participant Checkout as CheckoutOrderCommand
    participant Wallet
    participant VNPay
    participant Callback as ProcessVnPayCallbackCommand
    participant Escrow
    participant Warehouse as OutboundShipment
    participant GHN
    participant DecisionJob as ReleaseExpiredDecisionWindowJob
    participant Settlement as EscrowSettlementService

    Auction->>Auction: EndAuction -> Resolve(Sold)
    Auction->>AuctionSoldHandler: AuctionSoldEvent
    AuctionSoldHandler->>Order: Order.Create() (PendingPayment)
    Note over Order: PaymentDueAt = now + 48h

    Winner->>Checkout: POST /api/payments/checkout

    alt PaymentMethod = "vnpay"
        Checkout->>VNPay: CreateVnPayPaymentUrl (amount = totalAmount)
        Checkout-->>Winner: PaymentUrl
        Winner->>VNPay: Complete payment
        VNPay->>Callback: IPN callback (purpose=OrderPayment)
        Callback->>Escrow: Create escrow (VNPay amount)
        Callback->>Order: MarkAsPaid()
    else PaymentMethod = "wallet"
        Checkout->>Wallet: Check balance >= remaining
        Checkout->>Wallet: ConvertDeposit + DebitPending + Debit
        Checkout->>Escrow: Create escrow (full amount)
        Checkout->>Order: MarkAsPaid()
        Checkout-->>Winner: PaymentUrl = null
    else PaymentMethod = "wallet_vnpay"
        Checkout->>Wallet: Hold(walletPortion)
        Checkout->>VNPay: CreateVnPayPaymentUrl (vnpayPortion)
        Checkout-->>Winner: PaymentUrl
        Winner->>VNPay: Complete payment
        VNPay->>Callback: IPN callback
        Callback->>Wallet: DebitPending(walletHold)
        Callback->>Escrow: Create escrow (full amount)
        Callback->>Order: MarkAsPaid()
    end

    Warehouse->>GHN: Book outbound shipment
    GHN-->>Warehouse: Carrier picks up
    Warehouse->>Order: OutboundShipmentPickedUpEvent -> MarkAsShipped()
    GHN-->>Warehouse: Delivery confirmed
    Warehouse->>Order: OutboundShipmentDeliveredEvent -> MarkAsDelivered()
    Note over Order: DecisionWindowEndsAt = deliveredAt + 7 days

    alt Buyer requests return within window
        Winner->>Order: POST /api/orders/{id}/returns
        Note over Order: Return flow (see return subflow docs)
    else No return or dispute
        DecisionJob->>Settlement: ReleaseToSellerAsync()
        Settlement->>Escrow: ReleaseToSeller()
        Settlement->>Order: Complete()
    end
```

## Subflow Index

| # | File | Description |
|---|------|-------------|
| 01 | [01-auto-create-from-auction.md](01-auto-create-from-auction.md) | Order creation from buy-now and auction-won paths |
| 02 | [02-checkout-payment.md](02-checkout-payment.md) | Checkout payment command (3 methods) |
| 03 | [03-payment-callback.md](03-payment-callback.md) | VNPay callback for order payment |
| 04 | [04-cancel-expired.md](04-cancel-expired.md) | CancelExpiredOrdersJob background job |
| 05 | [05-shipping-delivery.md](05-shipping-delivery.md) | Shipping and delivery via GHN webhooks |

## Key Entities

| Entity | Aggregate Root | Description |
|--------|----------------|-------------|
| `Order` | Yes | Core order aggregate with status, pricing, shipping snapshot, payment timestamps |
| `OrderReturn` | No (owned by Order) | 1:1 via `uq_order_returns_order`. Tracks return request through approval, shipping, receipt |
| `Escrow` | Yes | Holds funds in escrow per order. Multiple escrows per order (VNPay + deposit). Status: Holding -> Released/Refunded |
| `OutboundShipment` | Yes | Warehouse-to-buyer shipment. Lifecycle: Pending -> Booked -> PickedUp -> InTransit -> Delivered |

## Background Jobs

| Job | Interval | Batch Size | Description |
|-----|----------|------------|-------------|
| `CancelExpiredOrdersJob` | 5 min | 100 | Cancels PendingPayment orders past PaymentDueAt. Marks auction as payment_defaulted, creates UserRiskFlag ("non_payment", Medium), MonitoringAlert, checks auto-suspend threshold |
| `ReleaseExpiredDecisionWindowJob` | 10 min | 100 | Finds Delivered orders with expired DecisionWindowEndsAt (no active return/dispute). Calls `EscrowSettlementService.ReleaseToSellerAsync()` to release escrow and mark order Completed |

## All Endpoints

| Method | Route | Handler | Auth | Description |
|--------|-------|---------|------|-------------|
| GET | `api/orders/{orderId}` | `GetOrderByIdQuery` | Yes | Get order details by ID |
| GET | `api/me/orders` | `GetMyOrdersQuery` | Yes | List current user's orders |
| POST | `api/payments/checkout` | `CheckoutOrderCommand` | Yes | Checkout order (vnpay / wallet / wallet_vnpay) |
| POST | `api/payments/vnpay/ipn` | `ProcessVnPayCallbackCommand` | No | VNPay IPN callback (order payment path) |
| POST | `api/orders/{orderId}/returns` | `RequestOrderReturnCommand` | Yes | Create return request (buyer, within decision window) |
| POST | `api/orders/{orderId}/returns/{returnId}/approve` | `ApproveOrderReturnCommand` | Yes | Approve return request |
| POST | `api/orders/{orderId}/returns/{returnId}/reject` | `RejectOrderReturnCommand` | Yes | Reject return request (reason required) |
| POST | `api/orders/{orderId}/returns/{returnId}/ship` | `ShipOrderReturnCommand` | Yes | Record return shipment (providerCode + trackingNumber) |
| POST | `api/orders/{orderId}/returns/{returnId}/confirm-received` | `ConfirmOrderReturnReceivedCommand` | Yes | Seller confirms return item received |
| POST | `webhooks/ghn` | `ProcessTrackingWebhookCommand` | No | GHN carrier webhook (updates OutboundShipment status) |

## Domain Events

| Event | Raised By | Description |
|-------|-----------|-------------|
| `OrderCancelledEvent` | `Order.Cancel()` | Fired when order is cancelled (includes OrderId, BuyerId, OrderNumber, Reason) |
| `AuctionSoldEvent` | Auction grain | Auction ended with a winner. Triggers `AuctionSoldEventHandler` which creates the order |
| `OutboundShipmentCreatedEvent` | `OutboundShipment.Create()` | Shipment created for an order |
| `OutboundShipmentBookedEvent` | `OutboundShipment.RecordBooked()` | Carrier confirmed booking, tracking number assigned |
| `OutboundShipmentPickedUpEvent` | `OutboundShipment.RecordPickedUp()` | Carrier picked up from warehouse. Triggers `OrderMarkedShippedEventHandler` |
| `OutboundShipmentDeliveredEvent` | `OutboundShipment.RecordDelivered()` | Carrier confirmed delivery. Triggers `OrderMarkedDeliveredEventHandler` |
| `OutboundShipmentFailedEvent` | `OutboundShipment.RecordFailed()` | Shipment delivery failed |
| `OutboundShipmentReturningEvent` | `OutboundShipment.RecordReturning()` | Package returning to sender |
| `OutboundShipmentReturnedEvent` | `OutboundShipment.RecordReturned()` | Package returned to warehouse |
| `OutboundShipmentCancelledEvent` | `OutboundShipment.Cancel()` | Shipment cancelled (only before PickedUp) |
| `OutboundTrackingEventRecordedEvent` | `OutboundShipment.RecordTrackingEvent()` | Any carrier tracking status update recorded |

## Configuration

| Key | Default | Source |
|-----|---------|--------|
| `Order:ReturnDecisionWindowDays` | `7` | appsettings.json / `IRuntimeSettings.Order.ReturnDecisionWindowDays` |
| `Ops:AutoSuspendAfterNonPaymentCount` | (configurable) | `IRuntimeSettings.Ops.AutoSuspendAfterNonPaymentCount` |
| Payment deadline (auction winner) | 48 hours | `AuctionSoldEventHandler.PaymentDeadlineHours` constant |
