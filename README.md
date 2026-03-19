# OIO Auction Platform

OIO la backend cho nen tang dau gia truc tuyen, gom cac module lon: user/auth, catalog item, auction realtime, payment-wallet-order, moderation/dispute/report, warehouse va notification.

---

## System Architecture

### C4 Context Diagram

```mermaid
C4Context
    title OIO Auction Platform - System Context

    Person(buyer, "Buyer", "Browses, bids, pays, receives items")
    Person(seller, "Seller", "Lists items, manages auctions")
    Person(admin, "Admin", "Moderates, resolves disputes")

    System(oio, "OIO Auction Platform", "Online auction system with real-time bidding, escrow payments, and warehouse verification")

    System_Ext(vnpay, "VNPay", "Payment gateway")
    System_Ext(ghn, "GHN", "Shipping carrier (Giao Hang Nhanh)")
    System_Ext(cloudinary, "Cloudinary", "Media storage and CDN")
    System_Ext(ekyc, "VNPT eKYC", "Identity verification")
    System_Ext(smtp, "Email (SMTP)", "Transactional emails")
    System_Ext(seq, "Seq", "Structured logging")

    Rel(buyer, oio, "Uses", "SPA / Mobile")
    Rel(seller, oio, "Uses", "SPA / Mobile")
    Rel(admin, oio, "Uses", "Admin dashboard")
    Rel(oio, vnpay, "Processes payments", "HTTPS")
    Rel(oio, ghn, "Creates shipments, receives webhooks", "HTTPS")
    Rel(oio, cloudinary, "Uploads/serves media", "HTTPS")
    Rel(oio, ekyc, "Verifies identity documents", "HTTPS")
    Rel(oio, smtp, "Sends emails", "SMTP")
    Rel(oio, seq, "Ships logs", "HTTPS")
```

### Container Diagram

```mermaid
C4Container
    title OIO Auction Platform - Container Diagram

    Person(client, "Client", "SPA / Mobile App")

    Container_Boundary(platform, "OIO Platform") {
        Container(api, "API Gateway", ".NET 10 Minimal API", "REST endpoints, auth, rate limiting")
        Container(signalr, "SignalR Hubs", "ASP.NET SignalR", "Real-time bidding, notifications, disputes")
        Container(orleans, "Orleans Grains", "Microsoft Orleans", "Single-threaded auction state, bid processing")
        Container(jobs, "Background Jobs", "Quartz.NET", "Auction activation/ending, cleanup, escrow release")
        ContainerDb(db, "PostgreSQL", "Database", "All domain data, EF Core")
        ContainerDb(redis, "Redis", "Cache + PubSub", "Session, SignalR backplane, distributed cache")
    }

    System_Ext(vnpay, "VNPay", "Payment gateway")
    System_Ext(ghn, "GHN", "Shipping")
    System_Ext(cloudinary, "Cloudinary", "Media CDN")
    System_Ext(ekyc, "VNPT eKYC", "Identity verification")
    System_Ext(smtp, "SMTP", "Email delivery")
    System_Ext(seq, "Seq", "Logging")

    Rel(client, api, "REST calls", "HTTPS")
    Rel(client, signalr, "WebSocket", "WSS")
    Rel(api, orleans, "Grain calls", "In-process")
    Rel(signalr, orleans, "PlaceBid, BuyNow", "In-process")
    Rel(api, db, "Reads/Writes", "EF Core")
    Rel(orleans, db, "Persist state", "EF Core")
    Rel(jobs, db, "Scheduled work", "EF Core")
    Rel(api, redis, "Cache, sessions", "StackExchange.Redis")
    Rel(signalr, redis, "Backplane", "StackExchange.Redis")
    Rel(api, vnpay, "Payment URLs, IPN", "HTTPS")
    Rel(jobs, ghn, "Create/track shipments", "HTTPS")
    Rel(api, cloudinary, "Upload media", "HTTPS")
    Rel(api, ekyc, "Verify identity", "HTTPS")
    Rel(jobs, smtp, "Send emails", "SMTP")
    Rel(api, seq, "Structured logs", "Serilog")
```

### Component Diagram (Clean Architecture)

```mermaid
graph TB
    subgraph Presentation ["Presentation Layer"]
        API["Minimal API Endpoints"]
        Hubs["SignalR Hubs<br/>(Auction, Notification, Dispute)"]
        Filters["Hub Filters<br/>(Idempotency, Auth)"]
    end

    subgraph Application ["Application Layer"]
        Commands["Commands<br/>(PlaceBid, CreateAuction, ...)"]
        Queries["Queries<br/>(GetAuction, GetMyBids, ...)"]
        Events["Domain Event Handlers<br/>(BidPlaced, AuctionEnded, ...)"]
        DTOs["DTOs / Mappings"]
        Validators["FluentValidation"]
        Behaviors["MediatR Behaviors<br/>(Validation, Logging, Tx)"]
    end

    subgraph Domain ["Domain Layer"]
        Aggregates["Aggregates<br/>(Auction, Order, User, ...)"]
        Entities["Entities<br/>(Bid, AutoBid, Escrow, ...)"]
        ValueObjects["Value Objects<br/>(Money, AuctionPricing, ...)"]
        DomainServices["Domain Services"]
        DomainEvents["Domain Events"]
        Enums["Smart Enums<br/>(AuctionStatus, OrderStatus, ...)"]
    end

    subgraph Infrastructure ["Infrastructure Layer"]
        Persistence["Persistence<br/>(EF Core, Repositories)"]
        Grains["Orleans Grains<br/>(IAuctionGrain)"]
        Payment["Payment Providers<br/>(VnPayGateway)"]
        Shipping["Shipping Providers<br/>(GHN Integration)"]
        Identity["Identity / eKYC<br/>(VNPT eKYC)"]
        Media["Media Provider<br/>(Cloudinary)"]
        Scheduling["Job Scheduling<br/>(Quartz.NET Jobs)"]
        Email["Email Service<br/>(SMTP)"]
        Caching["Caching<br/>(Redis)"]
    end

    Presentation --> Application
    Application --> Domain
    Infrastructure --> Domain
    Infrastructure --> Application

    style Presentation fill:#4a9eff,color:#fff
    style Application fill:#47b881,color:#fff
    style Domain fill:#e6a817,color:#fff
    style Infrastructure fill:#e25d5d,color:#fff
```

---

## Core Flow Diagrams

### Authentication Flow

```mermaid
sequenceDiagram
    autonumber
    participant Client
    participant API
    participant DB
    participant Email
    participant Redis

    rect rgb(230, 245, 255)
        Note over Client,Email: Registration
        Client->>API: POST /auth/register (email, password)
        API->>DB: Create User (status=Active) + Wallet + Profile
        API->>Email: Send verification email (token)
        API-->>Client: 200 OK {message: "Check email"}
    end

    rect rgb(230, 255, 230)
        Note over Client,DB: Email Confirmation
        Client->>API: POST /auth/confirm-email (token)
        API->>DB: Set EmailConfirmed = true
        API-->>Client: 200 OK
    end

    rect rgb(255, 245, 230)
        Note over Client,Redis: Login (no 2FA)
        Client->>API: POST /auth/login (email, password)
        API->>DB: Validate credentials
        API->>DB: Create UserSession + RefreshToken
        API-->>Client: 200 {accessToken, refreshToken, session}
    end

    rect rgb(255, 230, 230)
        Note over Client,Redis: Login (with TOTP 2FA)
        Client->>API: POST /auth/login (email, password)
        API->>DB: Validate credentials, detect TwoFactorEnabled
        API-->>Client: 200 {requiresTwoFactor: true, limitedToken}
        Client->>API: POST /auth/two-factor/verify (limitedToken, totpCode)
        API->>DB: Verify TOTP against TotpSecretKey
        API->>DB: Create UserSession + RefreshToken
        API-->>Client: 200 {accessToken, refreshToken, session}
    end
```

### Auction State Machine

```mermaid
stateDiagram-v2
    [*] --> Draft: create

    Draft --> Pending: submit
    Draft --> Cancelled: cancel

    Pending --> Approved: admin approve
    Pending --> Cancelled: cancel

    Approved --> Scheduled: publish (future start)
    Approved --> Cancelled: cancel

    Scheduled --> Active: ActivateAuctionJob
    Scheduled --> Cancelled: cancel
    Scheduled --> Sold: buy-now
    Scheduled --> Terminated: terminate

    Active --> Ended: EndAuctionJob
    Active --> Cancelled: cancel / emergency
    Active --> Terminated: terminate
    Active --> Sold: buy-now

    Ended --> Sold: resolve (has winner)
    Ended --> Failed: resolve (no bids / reserve not met)
    Ended --> Terminated: terminate

    Sold --> PaymentDefaulted: payment timeout
    Sold --> Terminated: terminate

    PaymentDefaulted --> Sold: relist (runner-up accepts)
    PaymentDefaulted --> Scheduled: relist (re-schedule)
    PaymentDefaulted --> Terminated: terminate
```

### Bidding Flow (SignalR + Orleans)

```mermaid
sequenceDiagram
    autonumber
    participant Bidder
    participant Hub as SignalR AuctionHub
    participant Grain as Orleans AuctionGrain
    participant DB
    participant Notify as Hub Broadcast

    Bidder->>Hub: PlaceBid(auctionId, amount, currency, idempotencyKey)
    Hub->>Grain: PlaceBidAsync(bidderId, amount, ipAddress)

    Note over Grain: Single-threaded per auction (no race conditions)

    alt Bid valid
        Grain->>Grain: Create Bid, mark previous as Outbid, update price
        Grain->>DB: Persist state
        Grain-->>Hub: Success(BidDto)
        Hub->>Notify: Broadcast BidPlaced to auction group
    else Bid invalid
        Grain-->>Hub: Failure(error)
        Hub-->>Bidder: HubCommandResult(error)
    end
```

### Item State Machine

```mermaid
stateDiagram-v2
    [*] --> Draft: create

    Draft --> PendingReview: submit (standard)
    Draft --> PendingVerify: submit (verifyByPlatform)

    PendingReview --> Approved: admin approve
    PendingReview --> Rejected: admin reject

    PendingVerify --> Approved: inspection approve (match)
    PendingVerify --> Rejected: inspection reject
    PendingVerify --> PendingConditionConfirmation: condition differs

    PendingConditionConfirmation --> Approved: seller confirms

    Rejected --> PendingVerify: re-submit
    Rejected --> PendingReview: re-submit

    Approved --> InAuction: auction uses item
    Active --> InAuction: auction uses item

    InAuction --> Sold: auction sold
    InAuction --> Active: auction ended (no sale)
```

### Order State Machine

```mermaid
stateDiagram-v2
    [*] --> PendingPayment: auction won / order created

    PendingPayment --> Paid: MarkAsPaid (payment confirmed)
    PendingPayment --> Cancelled: Cancel (payment timeout)

    Paid --> Shipped: MarkAsShipped (carrier picks up)
    Paid --> Disputed: MarkAsDisputed

    Shipped --> Delivered: MarkAsDelivered (carrier confirms)
    Shipped --> Disputed: MarkAsDisputed

    Delivered --> Completed: Complete (decision window expires)
    Delivered --> Disputed: MarkAsDisputed
    Delivered --> Refunded: MarkAsRefunded

    Disputed --> Refunded: dispute resolved (buyer wins)
    Disputed --> Completed: dispute resolved (seller wins)

    Completed --> Refunded: MarkAsRefunded (post-completion)
```

### Payment Flow (VNPay + Wallet)

```mermaid
sequenceDiagram
    autonumber
    participant Buyer
    participant API
    participant DB
    participant VNPay

    Buyer->>API: POST /payments/checkout (orderId, paymentMethod)

    alt paymentMethod = "vnpay"
        API->>DB: Create Transaction(status=Pending)
        API->>VNPay: Build payment URL
        API-->>Buyer: {paymentUrl}
        Buyer->>VNPay: Complete payment
        VNPay->>API: IPN callback (success)
        API->>DB: Transaction=Completed, Order=Paid, Create Escrow
    else paymentMethod = "wallet"
        API->>DB: Check wallet balance
        API->>DB: Wallet.Debit(amount)
        API->>DB: Transaction=Completed, Order=Paid, Create Escrow
        API-->>Buyer: {paymentUrl: null, status: completed}
    else paymentMethod = "wallet_vnpay" (hybrid)
        API->>DB: Wallet.Hold(walletPortion)
        API->>VNPay: Build URL for remaining amount
        API-->>Buyer: {paymentUrl}
        VNPay->>API: IPN callback
        API->>DB: Wallet.DebitPending(walletPortion)
        API->>DB: Transaction=Completed, Order=Paid, Create Escrow
    end
```

### Warehouse Flow (Inbound → Inspect → Store → Outbound)

```mermaid
sequenceDiagram
    autonumber
    participant Seller
    participant API
    participant GHN as GHN Carrier
    participant Inspector as Warehouse Staff
    participant DB
    participant Buyer

    rect rgb(230, 245, 255)
        Note over Seller,DB: Phase 1 - Inbound
        Seller->>API: Book inbound shipment
        API->>GHN: Create shipping order
        GHN->>API: Webhook: delivered to warehouse
        API->>DB: InboundShipment.Status = Arrived
    end

    rect rgb(255, 245, 230)
        Note over Inspector,DB: Phase 2 - Inspection
        Inspector->>API: Inspect item (condition, photos)
        API->>DB: WarehouseInspection created
        Inspector->>API: Review (approve/reject)
        API->>DB: Item.Status = Approved
    end

    rect rgb(230, 255, 230)
        Note over Inspector,DB: Phase 3 - Storage
        Inspector->>API: Assign storage location
        API->>DB: WarehouseItem.Status = Stored
    end

    rect rgb(245, 230, 255)
        Note over API,Buyer: Phase 4 - Outbound (after payment)
        API->>GHN: Create outbound shipping order
        GHN->>API: Webhook: delivered to buyer
        API->>DB: Order.MarkAsDelivered()
    end
```

---

## Kien truc code

```
src/
├── core/
│   ├── OIO.Domain          # Domain model, aggregate, enum, value object
│   └── OIO.Application     # Command/query, DTO, service, event handler
├── infrastructure/
│   └── OIO.Infrastructure   # Persistence, provider, settings, integration
└── presentation/
    └── OIO.Api              # HTTP API, SignalR hub, OpenAPI/Scalar
```

## Chay local

1. Cap nhat .env neu can.
2. Khoi dong dependency bang `compose.yaml` va `compose.override.yaml`.
3. Chay API tu `src/presentation/OIO.Api`.
4. Trong development, OpenAPI tai `/openapi/v1.json` va Scalar tai `/docs`.

## Auth conventions

- API mac dinh dung Bearer JWT.
- Route `api/admin/*` danh cho admin.
- Mot so route POST duoc gate boi Idempotency-Key.
- SignalR hubs deu can auth.

### Two-Factor Authentication (TOTP)

1. `POST /api/me/two-factor/setup` — Tao TOTP secret + QR code
2. `POST /api/me/two-factor/confirm` — Xac nhan → nhan 8 recovery codes
3. `POST /api/auth/login` tra ve limited JWT (3 phut) khi 2FA bat
4. `POST /api/auth/two-factor/verify` — Xac thuc TOTP → nhan full JWT

## Tai lieu

### API Documentation
- [API index](./docs/api/README.md) | [User + Auth](./docs/api/user.md) | [Auction + Catalog](./docs/api/auction.md) | [Payment + Order](./docs/api/payment-order.md) | [Moderation + Warehouse](./docs/api/moderation-warehouse.md) | [SignalR](./docs/api/signalr.md) | [Schemas](./docs/api/schemas.md)

### Business Flows (15 modules, 130+ docs)
- [Flow index](./docs/flows/README.md)

| # | Module | Docs |
|---|--------|------|
| 01 | [Registration & Auth](./docs/flows/01-registration-auth/README.md) | Register, login, 2FA, sessions |
| 02 | [User Profile](./docs/flows/02-user-profile/README.md) | Profile, phone, addresses, notification prefs |
| 03 | [Seller Verification](./docs/flows/03-seller-verification/README.md) | eKYC, documents, admin review |
| 04 | [Media Upload](./docs/flows/04-media-upload/README.md) | Cloudinary signed upload |
| 05 | [Item Management](./docs/flows/05-item-management/README.md) | Create, submit, review, QA |
| 06 | [Auction Lifecycle](./docs/flows/06-auction-lifecycle/README.md) | Draft → Publish → Active → Sold |
| 07 | [Bidding](./docs/flows/07-bidding/README.md) | Manual, auto-bid, sealed bid |
| 08 | [Buy Now](./docs/flows/08-buy-now/README.md) | Reserve → pay → finalize |
| 09 | [Payment](./docs/flows/09-payment/README.md) | VNPay, token, wallet, refund |
| 10 | [Order Lifecycle](./docs/flows/10-order-lifecycle/README.md) | Pay → ship → deliver → complete |
| 11 | [Warehouse](./docs/flows/11-warehouse-shipping/README.md) | Inbound, inspect, store, outbound |
| 12 | [Dispute](./docs/flows/12-dispute-moderation/README.md) | Reports, disputes, emergency |
| 13 | [Notification](./docs/flows/13-notification/README.md) | Email, SignalR, preferences |
| 14 | [Wallet](./docs/flows/14-wallet-withdrawal/README.md) | Topup, hold, withdraw |
| 15 | [Admin](./docs/flows/15-admin-operations/README.md) | Users, roles, review, monitoring |

### UML Diagrams
- [System Architecture](./docs/diagrams/system-architecture.md)
- [Entity Relationships](./docs/diagrams/entity-relationships.md)
- [Auth Flow](./docs/diagrams/flows/01-auth.md) | [Item](./docs/diagrams/flows/05-item.md) | [Auction](./docs/diagrams/flows/06-auction.md) | [Bidding](./docs/diagrams/flows/07-bidding.md) | [Payment](./docs/diagrams/flows/09-payment.md) | [Order](./docs/diagrams/flows/10-order.md) | [Warehouse](./docs/diagrams/flows/11-warehouse.md)

### Postman Collections (15 collections, 176+ requests)
Located in [`postman/`](./postman/) directory — import into Postman to test all flows.

## Regenerate docs

- Chay `scripts/generate-api-docs.ps1` de regenerate docs tu source hien tai.
- Lan dau: `scripts/generate-api-docs.ps1 -BootstrapDescriptions`.
