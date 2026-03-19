# 06 - Auction Lifecycle

## Tong quan

Module nay quan ly toan bo vong doi cua mot phien dau gia, tu khi tao ban nhap cho den khi ket thuc va xac dinh ket qua (Ban thanh cong / That bai / Vi pham thanh toan). Moi phien dau gia gan voi mot `Item` da duoc duyet, va trang thai chuyen tiep theo mot state machine nghiem ngat.

## Actors

- **Seller (Nguoi ban):** tao, cau hinh, submit, publish, cancel, relist phien dau gia
- **Admin:** duyet/tu choi item, set curation, trigger emergency, reveal sealed bids
- **System (Quartz Jobs):** tu dong kich hoat phien dau gia khi den gio, tu dong ket thuc khi het gio
- **Bidder (Nguoi mua):** dat gia, buy-now, tham gia qualification

## State Machine

```
                              +--------+
                              |  Draft |
                              +---+----+
                                  |
                          SubmitConfiguration()
                    (Item phai o trang thai Approved)
                                  |
                   +--------------+--------------+
                   |                             |
           [co AuctionInfo]             [khong co AuctionInfo]
                   |                             |
            +------v------+              +-------v-------+
            |  Scheduled  |              |   Approved    |
            +------+------+              +-------+-------+
                   |                             |
           ActivateAuctionJob             SetTiming()
         (kiem tra participants)                  |
                   |                      +------v------+
           +-------+-------+             |  Scheduled  |
           |               |             +------+------+
    [co participants]  [khong co]               |
           |               |          ActivateAuctionJob
    +------v------+   Auto Cancel             ...
    |   Active    |
    +------+------+
           |
     EndAuctionJob
     (End + Resolve)
           |
    +------v------+
    |    Ended    |
    +------+------+
           |
     +-----+------+
     |             |
[co winner &   [khong co bid
 reserve met]   hoac reserve
     |          khong met]
+----v----+   +----v----+
|   Sold  |   |  Failed |
+----+----+   +---------+
     |
[winner khong
 thanh toan]
     |
+----v-----------+
| PaymentDefaulted|
+----+-----+-----+
     |     |
  Relist  OfferRunnerUp
     |     |
     v     v
 Scheduled / Sold
```

### Bang chuyen trang thai (tu AuctionStatus.CanTransitionTo)

| Tu trang thai      | Den trang thai    | Trigger                                        |
|---------------------|-------------------|-------------------------------------------------|
| `draft`            | `pending`         | `SubmitConfiguration()` (khi Item can duyet)   |
| `draft`            | `cancelled`       | `CancelAuction()`                              |
| `pending`          | `approved`        | `MarkApproved()` (admin duyet)                 |
| `pending`          | `cancelled`       | `CancelAuction()`                              |
| `approved`         | `scheduled`       | `SetTiming()` hoac `UpdateConfiguration()`     |
| `approved`         | `cancelled`       | `CancelAuction()`                              |
| `scheduled`        | `active`          | `Start()` via ActivateAuctionJob               |
| `scheduled`        | `cancelled`       | `CancelAuction()` hoac auto-cancel (no participants) |
| `scheduled`        | `sold`            | Buy-now finalize (thanh toan buy-now thanh cong)|
| `scheduled`        | `terminated`      | Emergency terminate                            |
| `active`           | `ended`           | `End()` via EndAuctionJob                      |
| `active`           | `cancelled`       | `CancelAuction()`                              |
| `active`           | `terminated`      | `Terminate()` emergency                        |
| `active`           | `sold`            | Buy-now finalize (thanh toan buy-now thanh cong)|
| `ended`            | `sold`            | `Resolve()` - co winner va reserve met         |
| `ended`            | `failed`          | `Resolve()` - khong co bid hoac reserve not met|
| `ended`            | `terminated`      | Emergency terminate                            |
| `sold`             | `payment_defaulted`| `MarkPaymentDefaulted()`                      |
| `sold`             | `terminated`      | Emergency terminate                            |
| `payment_defaulted`| `sold`            | `TransferToRunnerUp()` hoac relist             |
| `payment_defaulted`| `scheduled`       | `RelistAuction()` tao phien moi                |
| `payment_defaulted`| `terminated`      | Emergency terminate                            |

## Subflow Files

| File                                                  | Mo ta                                           |
|-------------------------------------------------------|--------------------------------------------------|
| [create-auction.md](./create-auction.md)              | Tao phien dau gia (2 cach)                      |
| [set-timing-pricing.md](./set-timing-pricing.md)      | Dat lich va cau hinh gia                        |
| [submit-publish.md](./submit-publish.md)              | Submit cau hinh va Publish de len lich           |
| [activation.md](./activation.md)                      | Kich hoat phien dau gia (Quartz + Service)      |
| [auto-extension.md](./auto-extension.md)              | Tu dong gia han khi co bid gan ket thuc         |
| [end-resolve.md](./end-resolve.md)                    | Ket thuc va xac dinh ket qua                   |
| [cancel-auction.md](./cancel-auction.md)              | Huy phien dau gia                               |
| [relist-auction.md](./relist-auction.md)               | Dang lai phien dau gia                          |
| [runner-up-offer.md](./runner-up-offer.md)             | De nghi nguoi dat gia thu 2                     |

## Domain Events

| Event                          | Khi nao                                  |
|--------------------------------|------------------------------------------|
| `AuctionCreatedEvent`          | Tao phien dau gia moi                    |
| `AuctionSubmittedEvent`        | Submit cau hinh                          |
| `AuctionScheduledEvent`        | Dat lich (SetTiming / Submit co Info)     |
| `AuctionApprovedEvent`         | Admin duyet                              |
| `AuctionRejectedEvent`         | Admin tu choi                            |
| `AuctionStartedEvent`          | Kich hoat phien dau gia                  |
| `AuctionEndedEvent`            | Ket thuc phien dau gia                   |
| `AuctionSoldEvent`             | Xac dinh nguoi thang                     |
| `AuctionFailedEvent`           | That bai (khong co bid / reserve not met)|
| `AuctionCancelledEvent`        | Huy phien dau gia                        |
| `AuctionTerminatedEvent`       | Emergency terminate                      |
| `AuctionPaymentDefaultedEvent` | Winner khong thanh toan                  |
| `AuctionExtendedEvent`         | Gia han thoi gian                        |
| `AuctionFeatureToggledEvent`   | Bat/tat featured                         |

## Technology Stack

- **Orleans Grain:** `AuctionGrain` - dam bao single-threaded access per auction, tranh race conditions
- **Quartz.NET:** `ActivateAuctionJob`, `EndAuctionJob` - len lich kich hoat va ket thuc
- **MediatR:** Command/Handler pattern cho tat ca cac thao tac
- **Domain Events -> Outbox:** Cac domain event duoc chuyen thanh outbox messages qua SaveChanges interceptor
