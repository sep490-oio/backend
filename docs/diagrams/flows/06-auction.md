# Auction Lifecycle

## Auction Status State Machine

Derived directly from `AuctionStatus.CanTransitionTo()` in the domain.

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
    Scheduled --> Sold: buy-now (immediate)
    Scheduled --> Terminated: terminate

    Active --> Ended: EndAuctionJob
    Active --> Cancelled: cancel / emergency
    Active --> Terminated: terminate
    Active --> Sold: buy-now (immediate)

    Ended --> Sold: resolve (has winner)
    Ended --> Failed: resolve (no bids / reserve not met)
    Ended --> Terminated: terminate

    Sold --> PaymentDefaulted: payment timeout
    Sold --> Terminated: terminate

    PaymentDefaulted --> Sold: relist (runner-up accepts)
    PaymentDefaulted --> Scheduled: relist (re-schedule)
    PaymentDefaulted --> Terminated: terminate
```

### Transition Matrix (from code)

| From | To | Trigger |
|------|-----|---------|
| draft | pending | Submit auction |
| draft | cancelled | Seller cancels |
| pending | approved | Admin approves |
| pending | cancelled | Seller/admin cancels |
| approved | scheduled | Publish with future start time |
| approved | cancelled | Cancel before publish |
| scheduled | active | ActivateAuctionJob fires at start time |
| scheduled | cancelled | Cancel before activation |
| scheduled | sold | Buy-now completes |
| scheduled | terminated | Admin terminates |
| active | ended | EndAuctionJob fires at end time |
| active | cancelled | Cancel / emergency stop |
| active | terminated | Admin terminates |
| active | sold | Buy-now completes |
| ended | sold | Winner determined (highest bid >= reserve) |
| ended | failed | No bids or reserve not met |
| ended | terminated | Admin terminates |
| sold | payment_defaulted | Winner fails to pay within deadline |
| sold | terminated | Admin terminates |
| payment_defaulted | sold | Runner-up accepts offer |
| payment_defaulted | scheduled | Re-list for new auction |
| payment_defaulted | terminated | Admin terminates |

## Auction Lifecycle Sequence

```mermaid
sequenceDiagram
    autonumber
    participant Seller
    participant API
    participant Admin
    participant Job as Background Job
    participant Orleans as Orleans Grain
    participant Hub as SignalR Hub
    participant DB

    Seller->>API: Create auction (Draft)
    API->>DB: Auction(status=Draft)

    Seller->>API: Submit auction
    API->>DB: status = Pending

    Admin->>API: Approve auction
    API->>DB: status = Approved

    Seller->>API: Publish auction (future start)
    API->>DB: status = Scheduled
    API->>Job: Schedule ActivateAuctionJob

    Note over Job: At start time
    Job->>DB: status = Active
    Job->>Hub: Notify watchers "Auction started"

    Note over Orleans: During active period (see 07-bidding)

    Note over Job: At end time
    Job->>Orleans: EndAuctionAsync()
    Orleans->>DB: status = Ended, determine winner
    Orleans->>Hub: Broadcast "Auction ended"

    alt Has winner (bid >= reserve)
        Job->>DB: status = Sold, create Order
        Job->>Hub: Notify winner + seller
    else No valid bids
        Job->>DB: status = Failed
    end

    alt Winner does not pay
        Job->>DB: status = PaymentDefaulted
        Job->>DB: Create AuctionWinnerOffer for runner-up
        alt Runner-up accepts
            API->>DB: status = Sold (new winner)
        else Runner-up declines / offer expires
            API->>DB: status = Scheduled (re-list)
        end
    end
```
