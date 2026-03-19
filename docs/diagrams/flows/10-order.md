# Order Lifecycle

## Order Status State Machine

Derived from `Order.cs` transition methods (`MarkAsPaid`, `MarkAsShipped`, `MarkAsDelivered`, `Complete`, `Cancel`, `MarkAsDisputed`, `MarkAsRefunded`).

```mermaid
stateDiagram-v2
    [*] --> PendingPayment: auction won / order created

    PendingPayment --> Paid: MarkAsPaid (payment confirmed)
    PendingPayment --> Cancelled: Cancel (payment timeout / CancelExpiredOrdersJob)

    Paid --> Shipped: MarkAsShipped (carrier picks up)
    Paid --> Processing: processing begins
    Paid --> Disputed: MarkAsDisputed
    Paid --> Completed: Complete (direct, e.g. digital)
    Paid --> Refunded: MarkAsRefunded

    Processing --> Shipped: MarkAsShipped
    Processing --> Delivered: MarkAsDelivered (direct)
    Processing --> Completed: Complete

    Shipped --> Delivered: MarkAsDelivered (carrier confirms)
    Shipped --> Disputed: MarkAsDisputed

    Delivered --> Completed: Complete (decision window expires / ReleaseExpiredDecisionWindowJob)
    Delivered --> Disputed: MarkAsDisputed
    Delivered --> Refunded: MarkAsRefunded

    Disputed --> Refunded: dispute resolved (buyer wins)
    Disputed --> Completed: dispute resolved (seller wins)

    Completed --> Refunded: MarkAsRefunded (post-completion refund)

    Cancelled --> [*]
    Refunded --> [*]
    Completed --> [*]
```

### Transition Rules (from code)

| Method | Valid Source States | Target |
|--------|-------------------|--------|
| `MarkAsPaid` | PendingPayment | Paid |
| `MarkAsShipped` | Paid, Processing | Shipped |
| `MarkAsDelivered` | Shipped, Processing | Delivered |
| `Complete` | Delivered, Processing, Paid | Completed |
| `Cancel` | PendingPayment | Cancelled |
| `MarkAsDisputed` | Delivered, Paid, Shipped | Disputed |
| `MarkAsRefunded` | Delivered, Disputed, Completed, Paid | Refunded |

## Order Creation Sequence (after auction ends)

```mermaid
sequenceDiagram
    autonumber
    participant Job as EndAuctionJob
    participant DB
    participant Buyer
    participant API
    participant VNPay
    participant Escrow as Escrow Service
    participant Notify as Notifications

    Job->>DB: Auction.Status = Sold, set WinnerId
    Job->>DB: Create Order(status=PendingPayment, paymentDueAt=+48h)
    Job->>Notify: Notify winner "You won! Pay within 48h"
    Job->>Notify: Notify seller "Item sold!"

    Note over Buyer: Within 48 hours

    Buyer->>API: Initiate payment (VNPay / Wallet)
    API->>VNPay: Create payment URL
    Buyer->>VNPay: Complete payment
    VNPay->>API: IPN callback (success)
    API->>DB: Order.MarkAsPaid()
    API->>DB: Create Escrow(status=Holding)
    API->>Notify: Notify seller "Payment received, ship item"

    Note over DB: Seller ships from warehouse or direct

    API->>DB: Order.MarkAsShipped()
    API->>Notify: Notify buyer "Your order has shipped"

    Note over DB: Carrier delivers

    API->>DB: Order.MarkAsDelivered(decisionWindowEndsAt=+3d)
    API->>Notify: Notify buyer "Delivered! 3 days to inspect"

    alt Buyer accepts (or window expires)
        Job->>DB: Order.Complete()
        Job->>Escrow: Release to seller
        Job->>DB: Escrow.Status = ReleasedToSeller
    else Buyer requests return
        Buyer->>API: RequestReturn(reasonCode, description)
        API->>DB: Create OrderReturn(status=Requested)
    else Buyer opens dispute
        Buyer->>API: Open dispute
        API->>DB: Order.MarkAsDisputed()
        API->>DB: Escrow.Status = Disputed
    end
```

## Order Return Status State Machine

```mermaid
stateDiagram-v2
    [*] --> Requested: buyer requests return

    Requested --> Approved: seller/admin approves
    Requested --> Rejected: seller/admin rejects
    Requested --> Cancelled: buyer cancels

    Approved --> ReturnInTransit: buyer ships back
    Rejected --> [*]
    Cancelled --> [*]

    ReturnInTransit --> SellerReceived: seller confirms receipt

    SellerReceived --> Resolved: refund issued
    SellerReceived --> BuyerFollowup: issue with return

    BuyerFollowup --> Resolved: resolved

    Resolved --> [*]
```
