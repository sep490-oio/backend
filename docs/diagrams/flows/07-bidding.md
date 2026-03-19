# Bidding Flow

## Manual Bid Sequence

```mermaid
sequenceDiagram
    autonumber
    participant Bidder
    participant Hub as SignalR AuctionHub
    participant Filter as IdempotencyFilter
    participant MediatR
    participant Grain as Orleans AuctionGrain
    participant DB
    participant Notify as Hub Broadcast

    Bidder->>Hub: PlaceBid(auctionId, amount, currency, idempotencyKey)
    Hub->>Filter: Check idempotency key
    Filter->>MediatR: Send PlaceBidCommand

    MediatR->>Grain: PlaceBidAsync(bidderId, amount, ipAddress)

    Note over Grain: Single-threaded per auction (no race conditions)

    Grain->>Grain: Validate auction is Active
    Grain->>Grain: Validate amount >= currentPrice + bidIncrement
    Grain->>Grain: Validate bidder != seller
    Grain->>Grain: Validate bidder has deposit (if required)

    alt Bid valid
        Grain->>Grain: Create Bid(status=Active)
        Grain->>Grain: Mark previous highest Bid as Outbid
        Grain->>Grain: Update current price
        Grain->>Grain: Record AuctionPriceHistory
        Grain->>DB: Persist state
        Grain-->>MediatR: Success(BidDto)
        MediatR-->>Hub: BidDto
        Hub-->>Bidder: HubCommandResult(success, BidDto)
        Hub->>Notify: Broadcast BidPlaced to auction group
    else Bid invalid
        Grain-->>MediatR: Failure(error)
        MediatR-->>Hub: Error
        Hub-->>Bidder: HubCommandResult(error)
    end
```

## Auto-Bid Battle Sequence

```mermaid
sequenceDiagram
    autonumber
    participant BidderA as Bidder A
    participant BidderB as Bidder B
    participant Hub as SignalR Hub
    participant Grain as Orleans AuctionGrain
    participant DB

    Note over BidderA,DB: Setup phase
    BidderA->>Hub: ConfigureAutoBid(maxAmount=500, increment=10)
    Hub->>Grain: ConfigureAutoBidAsync(A, max=500, inc=10)
    Grain->>Grain: Create AutoBid(status=Active)
    Grain-->>Hub: OK

    BidderB->>Hub: ConfigureAutoBid(maxAmount=400, increment=15)
    Hub->>Grain: ConfigureAutoBidAsync(B, max=400, inc=15)
    Grain->>Grain: Create AutoBid(status=Active)
    Grain-->>Hub: OK

    Note over Grain: A manual bid triggers auto-bid cascade

    BidderA->>Hub: PlaceBid(amount=100)
    Hub->>Grain: PlaceBidAsync(A, 100)
    Grain->>Grain: Bid A=100 (Active), currentPrice=100

    Note over Grain: Auto-bid engine kicks in

    Grain->>Grain: B has AutoBid active, max=400
    Grain->>Grain: Create Bid B=110 (isAutoBid=true)
    Grain->>Grain: A outbid, A has AutoBid active, max=500
    Grain->>Grain: Create Bid A=125 (isAutoBid=true)
    Grain->>Grain: B outbid, B has AutoBid active, max=400
    Grain->>Grain: Create Bid B=135 (isAutoBid=true)

    Note over Grain: ... cascade continues ...

    Grain->>Grain: B=395 (isAutoBid=true)
    Grain->>Grain: A=405 (isAutoBid=true)
    Grain->>Grain: B max=400 exhausted
    Grain->>Grain: AutoBid B status=Exhausted

    Grain->>DB: Persist all bids + state
    Grain->>Hub: Broadcast all bid events to group

    Hub->>BidderA: BidPlaced notifications
    Hub->>BidderB: BidPlaced notifications + Outbid alert
```

## Bid Status State Machine

```mermaid
stateDiagram-v2
    [*] --> Active: bid placed
    Active --> Outbid: higher bid received
    Active --> Winning: auction ends (highest)
    Winning --> Won: auction resolved
    Active --> Cancelled: bid cancelled
    Outbid --> [*]
    Won --> [*]
    Cancelled --> [*]
```

## Auto-Bid Status State Machine

```mermaid
stateDiagram-v2
    [*] --> Active: configure
    Active --> Paused: user pauses
    Paused --> Active: user resumes
    Active --> Exhausted: max amount reached
    Active --> Won: auction ends (winning)
    Active --> Outbid: outbid beyond max
    Exhausted --> [*]
    Won --> [*]
    Outbid --> [*]
```

## Sealed Bid Status State Machine

```mermaid
stateDiagram-v2
    [*] --> Submitted: submit sealed bid
    Submitted --> Revealed: admin reveals at auction end
    Submitted --> Withdrawn: bidder withdraws
    Submitted --> Invalidated: validation failure
    Revealed --> [*]
    Withdrawn --> [*]
    Invalidated --> [*]
```

## Buy-Now Reservation Flow

```mermaid
stateDiagram-v2
    [*] --> PendingPayment: initiate buy-now
    PendingPayment --> Paid: payment succeeds
    PendingPayment --> Expired: reservation timeout (ExpireBuyNowReservationsJob)
    PendingPayment --> Cancelled: buyer cancels
    PendingPayment --> Failed: payment fails
    Paid --> [*]
    Expired --> [*]
    Cancelled --> [*]
    Failed --> [*]
```
