# Auto-Bid Configure

## Tong quan

Bidder cau hinh auto-bid de he thong tu dong dat gia khi bi outbid, den khi dat muc gia toi da. He thong hold so tien tuong ung trong wallet de dam bao co du tien.

## Actors

- **Bidder (Nguoi mua):** Cau hinh auto-bid

## Endpoint Sequence

### Cach 1: SignalR Hub (Primary)

- **Hub:** `/hubs/auction`
- **Method:** `ConfigureAutoBid(auctionId, maxAmount, currency, incrementAmount?)`
- **Auth:** Required (Permission: `Auctions.AutoBid`)
- **Response:** Error notification neu that bai

### Cach 2: REST API (Fallback)

- **Method:** `PUT api/auctions/{auctionId}/auto-bid`
- **Auth:** Required (Permission: `Auctions.AutoBid`)
- **Request:**
  ```json
  {
    "maxAmount": 2000000,
    "currency": "VND",
    "incrementAmount": 50000
  }
  ```
- **Response:** `200 OK` - `AutoBidDto`
- **Ghi chu:**
  - `maxAmount` bat buoc, `incrementAmount` tuy chon
  - Neu `incrementAmount` khong duoc cung cap, he thong su dung `BidIncrement` cua auction

## Business Logic (AuctionGrain.ConfigureAutoBidAsync)

```
Money.Create(maxAmount, currency)
    |
    v
auction.ValidateAutoBidConfiguration(bidder, maxAmount, nowUtc, incrementAmount)
    |
    v
[Validation passed?]
    |           |
  [Co]       [Khong] -> return Error
    |
    v
[Co existing auto-bid?]
    |                   |
  [Co]               [Khong]
    |                   |
  Calculate          holdDelta = maxAmount
  holdDelta =           |
  newMax - previousMax   |
    |                   |
    +-------------------+
              |
         [holdDelta != 0?]
              |           |
            [Co]       [Khong]
              |           |
    Load wallet           |
    Hold/Unhold           |
              |           |
              +-----+-----+
                    |
              auction.ConfigureAutoBid()
                    |
              [Existing?]
              |         |
        UpdateConfig  Create new AutoBid
              |         |
              +----+----+
                   |
         EngageAutoBidAgainstCurrentWinner()
                   |
              SaveAsync()
```

### Validation Rules

1. Auction phai `AcceptsBids` (Active, co timing, chua het han)
2. Khong co active buy-now reservation
3. Phai la live auction (khong phai Sealed)
4. Bidder khong phai seller
5. Bidder phai la qualified participant
6. `maxAmount >= GetMinimumBidAmount()`
7. `incrementAmount > 0` (neu co)
8. Neu update existing:
   - Auto-bid phai enabled (khong bi pause) hoac o status Exhausted/Outbid
   - Khong the modify auto-bid da Won
   - `newMaxAmount >= existing.Budget.CurrentAmount`

### Wallet Hold Logic

- **Tao moi:** `wallet.Hold(maxAmount)` - hold toan bo maxAmount
- **Tang maxAmount:** `wallet.Hold(newMax - oldMax)` - hold chenh lech
- **Giam maxAmount:** `wallet.Unhold(oldMax - newMax)` - tra lai chenh lech
- **Khong doi:** Skip hold/unhold

Neu wallet khong du so du: tra ve error `Wallet.HoldFailed`

### EngageAutoBidAgainstCurrentWinner

Sau khi cau hinh, neu auto-bid co the outbid current winner:
- Tinh toan bidAmount tiep theo
- Dat bid tu dong ngay lap tuc
- Dam bao auto-bid "enter the race" ngay khi cau hinh

## Domain Events

| Event                          | Payload                              |
|--------------------------------|--------------------------------------|
| `AuctionAutoBidConfiguredEvent`| AuctionId, BidderId, MaxAmount       |
| `BidPlacedEvent`               | Neu auto-bid dat gia ngay lap tuc    |

## Luu y nghiep vu

- Moi bidder chi co **mot** auto-bid per auction (UNIQUE: auction_id + bidder_id)
- Auto-bid co the duoc cap nhat nhieu lan (tang maxAmount, thay doi increment)
- Wallet hold dam bao bidder co du tien khi auto-bid chay
- Hold delta: chi hold/unhold chenh lech, khong hold lai toan bo
- Auto-bid co the dat gia ngay khi cau hinh neu dieu kien thoa man (outbid current winner)
- Sau khi cau hinh, auto-bid se tu dong chay khi co bid moi tu nguoi khac
