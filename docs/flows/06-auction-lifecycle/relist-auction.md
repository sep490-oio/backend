# Relist Auction

## Tong quan

Khi nguoi thang dau gia khong thanh toan (trang thai `PaymentDefaulted`), seller co the dang lai phien dau gia voi cau hinh moi. He thong tao mot Auction moi tu Auction cu, cho phep seller ghi de pricing va dat lich moi.

## Actors

- **Seller (Nguoi ban):** Dang lai phien dau gia

## Endpoint Sequence

### Step 1: Relist Auction

- **Method:** `POST api/auctions/{auctionId}/relist`
- **Auth:** Required (Seller, owner cua Item)
- **Request:**
  ```json
  {
    "qualificationStartAt": "2026-04-10T00:00:00Z",
    "qualificationEndAt": "2026-04-14T09:00:00Z",
    "startAt": "2026-04-15T10:00:00Z",
    "endAt": "2026-04-15T22:00:00Z",
    "startingPrice": 100000,
    "bidIncrement": 10000,
    "reservePrice": 500000,
    "buyNowPrice": 1000000,
    "currency": "VND",
    "reason": "Nguoi thang khong thanh toan"
  }
  ```
- **Response:** `200 OK` - `AuctionDto` (phien dau gia moi)
- **Ghi chu:**
  - `qualificationStartAt`, `qualificationEndAt`, `startAt`, `endAt` la **bat buoc**
  - `startingPrice`, `bidIncrement`, `reservePrice`, `buyNowPrice`, `currency` la tuy chon - neu khong cung cap se lay tu phien dau gia cu
  - `reason` la tuy chon

## Business Logic

```
Validate Auction exists
        |
Validate quyen (seller owner)
        |
[Status == PaymentDefaulted?]
        |              |
      [Co]          [Khong] -> Error: InvalidState("relist")
        |
[Da co relist truoc do (co NewAuctionId)?]
        |              |
      [Co]          [Khong]
        |              |
Error: AlreadyRelisted  |
                        |
        Tao QualificationWindow moi
                        |
        Tao AuctionInfo moi
                        |
        Tao AuctionPricing (merge: request overrides || phien cu)
                        |
        Auction.Create() -> Auction moi o trang thai Draft
                        |
        auction.RegisterRelist(newAuctionId, reason, nowUtc)
                        |
        SaveChanges
                        |
        Return AuctionDto cua phien moi
```

## Pricing Override Logic

| Field          | Quy tac                                      |
|----------------|----------------------------------------------|
| `startingPrice`| `request.StartingPrice ?? auction.Pricing.StartingAmount` |
| `bidIncrement` | `request.BidIncrement ?? auction.Pricing.BidIncrementAmount` |
| `reservePrice` | `request.ReservePrice ?? auction.Pricing.ReserveAmount`  |
| `buyNowPrice`  | `request.BuyNowPrice ?? auction.Pricing.BuyNowAmount`    |
| `currency`     | `request.Currency ?? auction.Pricing.Currency.Id`        |
| `auctionType`  | Ke thua tu phien cu (khong thay doi)                    |
| `autoExtend`   | Ke thua tu phien cu                                      |
| `extensionMinutes` | Ke thua tu phien cu (mac dinh 5)                     |

## Domain Events

| Event                | Payload                                              |
|----------------------|------------------------------------------------------|
| `AuctionCreatedEvent`| Phien dau gia moi duoc tao                           |

## Luu y nghiep vu

- Chi co the relist khi Auction o trang thai `PaymentDefaulted`
- Moi phien dau gia chi co the relist **mot lan** (kiem tra `RelistHistories.Any(x => x.NewAuctionId.HasValue)`)
- Phien dau gia moi duoc tao o trang thai `Draft` - seller phai submit va publish rieng
- `AuctionRelistHistory` ghi lai moi lien ket giua phien cu va phien moi
- Sealed auction khong ho tro autoExtend - he thong tu dong kiem tra
- Item goc van duoc giu nguyen (khong thay doi trang thai)
