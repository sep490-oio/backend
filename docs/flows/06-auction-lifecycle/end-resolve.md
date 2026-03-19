# End & Resolve Auction

## Tong quan

Khi het thoi gian, he thong tu dong ket thuc phien dau gia va xac dinh ket qua. Qua trinh gom hai buoc:
1. **End():** Chuyen trang thai tu `Active` sang `Ended`
2. **Resolve():** Xac dinh ket qua cuoi cung - `Sold` (co winner), `Failed` (khong co bid hoac reserve not met)

Doi voi Sealed auction, truoc khi End() va Resolve(), he thong se reveal tat ca sealed bids.

## Actors

- **System (Quartz Job):** `EndAuctionJob` duoc trigger theo lich
- **Admin:** Co the ket thuc phien dau gia thu cong (seller hoac admin)

## Endpoint Sequence

### Step 1: EndAuctionJob (Quartz) hoac Manual Close

**Automatic (EndAuctionJob):**
- **Trigger:** Quartz trigger tai thoi diem `EndTime`
- **Job Key:** `end-{auctionId}` trong group `AuctionLifecycleGroup`

**Manual:**
- **Method:** `POST api/auctions/{auctionId}/close`
- **Auth:** Required (Seller hoac Admin role `Catalogs.Admin`)

### Step 2: EndAuctionCommand Handler

1. Load Auction voi Item va SealedBids
2. Kiem tra quyen (seller hoac admin)
3. Neu Auction khong o trang thai `Active` -> return Success (idempotent)
4. **Neu la Sealed auction:**
   - Decrypt tat ca sealed bids bang `ISealedBidEncryptionService`
   - Truyen `revealedSealedBids` vao grain
5. Goi `IAuctionGrain.EndAuctionAsync(revealerId, revealedSealedBids)`

### Step 3: AuctionGrain.EndAuctionAsync

```
[Status != Active?] -> return Success (idempotent)
        |
[Co active BuyNow reservation?] -> return Success (cho reservation het han)
        |
[Sealed auction?]
   |           |
 [Co]        [Khong]
   |           |
RevealAllSealedBids()    |
   |                     |
   +---------------------+
              |
         End(nowUtc)
              |
       Status = Ended
       ActualEndTime = nowUtc
              |
         Resolve(nowUtc)
              |
    +---------+---------+
    |         |         |
[No bids]  [Reserve   [Winner +
    |       not met]   Reserve met]
    |         |         |
 Failed    Failed      Sold
```

### Resolve Logic Chi tiet

#### Case 1: Khong co bid
- Huy tat ca auto-bids (`TerminalizeAllAutoBids`)
- `MarkAsFailed()` -> Status = `Failed`
- Raise `AuctionFailedEvent` (reason: "No bids received")

#### Case 2: Co bid nhung reserve khong met
- Huy tat ca auto-bids
- Cancel tat ca winning bids
- `MarkAsFailed()` -> Status = `Failed`
- Raise `AuctionFailedEvent` (reason: "Reserve price not met")

#### Case 3: Co winner va reserve met
- Winning bid duoc `MarkAsWon()`
- Set `WinnerId = winningBid.BidderId`
- Cancel tat ca bids khac (Active, Winning, Outbid)
- `SyncAutoBidStateToWinner(winnerId)` - cap nhat trang thai auto-bid
- `MarkAsSold()` -> Status = `Sold`
- Raise `AuctionSoldEvent`

### Sealed Bid Reveal Process

Khi `RevealAllSealedBids()` duoc goi:
1. Tat ca sealed bids duoc `Reveal()` (cap nhat status)
2. Cac sealed bid co amount >= startingPrice duoc chuyen thanh `Bid` entities
3. Sap xep theo amount DESC, sau do theo thoi gian tao ASC
4. Bid cao nhat duoc danh dau `Winning`, con lai `Outbid`
5. Cap nhat Pricing voi gia cao nhat
6. Cap nhat BidCount

## Background Jobs

| Job               | Schedule                 | Mo ta                              |
|-------------------|--------------------------|------------------------------------|
| `EndAuctionJob`   | One-shot tai `EndTime`   | Tu dong ket thuc phien dau gia     |
| `AuctionAutoCompleteJob` | Recurring          | Safety fallback cho truong hop job miss |

## Domain Events

| Event                | Payload                                                    |
|----------------------|------------------------------------------------------------|
| `AuctionEndedEvent`  | AuctionId, WinnerId, FinalPrice, BidCount, ReserveMet      |
| `AuctionSoldEvent`   | AuctionId, WinnerId, SellerId, FinalPrice, Currency, TotalBids |
| `AuctionFailedEvent` | AuctionId, SellerId, Reason, FinalPrice, Currency, TotalBids  |

## SignalR Notifications

- `AuctionEnded(AuctionEndedNotification)` gui toi group `auction:{auctionId}`:
  ```json
  {
    "auctionId": "guid",
    "winnerId": "guid?",
    "winnerDisplayName": "string?",
    "finalPrice": 1500000,
    "totalBids": 25,
    "reserveMet": true
  }
  ```

## Luu y nghiep vu

- Neu co active BuyNow reservation khi EndAuctionJob chay, job return Success va doi `ExpireBuyNowReservationsJob` xu ly reservation het han, sau do trigger EndAuction lai
- Sealed auction bat buoc phai co revealed bids de End - khong co revealed bids -> loi validation
- End va Resolve xay ra trong cung mot transaction (trong AuctionGrain)
- Resolve chi goi duoc khi trang thai la `Ended` - dam bao thu tu thuc hien
- Bid marking (Won/Outbid) chi xay ra trong Resolve(), khong phai End() - tranh tinh trang bid bi danh dau Won khi auction that bai
