# Manual Bid (Dat gia thu cong)

## Tong quan

Nguoi mua dat gia thu cong voi so tien cu the. Co the thuc hien qua SignalR hub (real-time) hoac REST API (fallback). Moi bid duoc xu ly qua `AuctionGrain` de dam bao single-threaded access.

## Actors

- **Bidder (Nguoi mua):** Dat gia thu cong

## Endpoint Sequence

### Cach 1: SignalR Hub (Primary)

- **Hub:** `/hubs/auction`
- **Method:** `PlaceBid(auctionId, amount, currency, idempotencyKey?)`
- **Auth:** Required (Permission: `Auctions.Bid`)
- **Response:** `HubCommandResult<BidDto>`
  ```json
  {
    "isSuccess": true,
    "value": {
      "id": "guid",
      "auctionId": "guid",
      "bidderId": "guid",
      "amount": { "amount": 1500000, "currency": "VND" },
      "isAutoBid": false,
      "status": "winning",
      "createdAt": "2026-04-01T15:30:00Z"
    }
  }
  ```

### Cach 2: REST API (Fallback)

- **Method:** `POST api/auctions/{auctionId}/bids`
- **Auth:** Required (Permission: `Auctions.Bid`)
- **Request:**
  ```json
  {
    "amount": 1500000,
    "currency": "VND"
  }
  ```
- **Response:** `200 OK` - `BidDto`

## Business Logic (PlaceBidCommand -> AuctionGrain)

```
PlaceBidCommand
    |
    v
Money.Create(amount, currency)
    |
    v
AuctionGrain.PlaceBidAsync(bidderId, amount, ipAddress)
    |
    v
LoadAuctionAsync() -> Auction aggregate (cached)
    |
    v
auction.PlaceBid(bidderId, amount, nowUtc, extensionThreshold, maxExtensions, maxDuration, ipAddress)
```

### Domain Validation (trong thu tu)

1. **EnsureAcceptsBids:** Status phai la `Active`, co timing, chua het han
2. **EnsureNotLockedByBuyNowReservation:** Khong co active buy-now reservation
3. **EnsureLiveBiddingSupported:** Khong phai sealed auction
4. **EnsureNotSeller:** Bidder khong phai seller
5. **EnsureBidderEligible:** Bidder phai la qualified participant
6. **Kiem tra gia toi thieu:** `amount >= GetMinimumBidAmount()`
   - Neu khong co bid: minimum = `StartingPrice`
   - Neu co bid: minimum = `NextMinimumBid` (CurrentPrice + BidIncrement)

### Sau khi validation thanh cong

1. Bid truoc do (Winning) bi danh dau `Outbid` + raise `OutbidEvent`
2. Tao `Bid.Create()` va danh dau `Winning`
3. Cap nhat Pricing (CurrentPrice, NextMinimumBid)
4. Cap nhat BidCount, PriceHistory
5. **TryAutoExtend:** Gia han thoi gian neu bid gan ket thuc
6. Raise `BidPlacedEvent`
7. **ProcessAutoBids:** Trigger auto-bid cascade tu cac bidder khac

### Auto-bid Cascade

Sau moi manual bid, `ProcessAutoBids(excludeBidderId, nowUtc)` duoc goi:
- Tim cac auto-bid eligible (khac bidder vua dat, enabled, status Active)
- Sap xep theo maxAmount DESC, createdAt ASC
- Moi auto-bid co co hoi dat gia
- Neu auto-bid thanh cong, battle voi manual bidder's auto-bid (neu co)
- Xem chi tiet tai [auto-bid-battle.md](./auto-bid-battle.md)

## Domain Events

| Event            | Payload                                                          |
|------------------|------------------------------------------------------------------|
| `BidPlacedEvent` | AuctionId, BidId, BidderId, Amount, PreviousHighestBid, IsAutoBid, BidCount |
| `OutbidEvent`    | AuctionId, OutbidBidderId, NewHighBidderId, NewHighestBid, OutbidAmount |

## SignalR Notifications

### BidPlaced (gui toi group `auction:{auctionId}`)
```json
{
  "auctionId": "guid",
  "bidId": "guid",
  "bidderId": "guid",
  "bidderDisplayName": "Nguyen Van A",
  "amount": 1500000,
  "currentPrice": 1500000,
  "minimumNextBid": 1510000,
  "totalBids": 15,
  "isAutoBid": false,
  "timestamp": "2026-04-01T15:30:00Z"
}
```

### Outbid (gui toi user group `user:{outbidBidderId}`)
```json
{
  "auctionId": "guid",
  "newHighAmount": 1500000,
  "minimumNextBid": 1510000,
  "newHighBidderDisplayName": "Nguyen Van A"
}
```

## Invalid Bid Tracking

Khi bid that bai (validation error), `PlaceBidCommandHandler` tu dong:
1. Ghi `AuditLog` voi action `invalid_bid_attempt`
2. Kiem tra so lan that bai trong 10 phut gan nhat (theo userId hoac IP)
3. Neu vuot nguong `RuntimeSettings.Monitoring.InvalidBidBurstThreshold`:
   - Tao `MonitoringAlert` (type: `invalid_bid_burst`, severity: `High`)
   - Xem chi tiet tai [invalid-bid-detection.md](./invalid-bid-detection.md)

## Luu y nghiep vu

- Moi bid duoc xu ly single-threaded qua Orleans Grain -> khong co race conditions
- Grain cache auction aggregate trong memory, discard sau moi save
- IP address duoc ghi lai cho moi bid (audit va fraud detection)
- Bid dat gia thap hon minimum -> error `Bid.TooLow`
- Seller dat gia tren auction cua minh -> error `Auction.SelfBid`
- Bidder chua du dieu kien -> error `Participant.NotQualified`
