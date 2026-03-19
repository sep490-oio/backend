# Sealed Bid (Dat gia kin)

## Tong quan

Sealed auction cho phep bidder gui encrypted bid amount. Bid chi duoc reveal sau khi phien dau gia ket thuc. Admin co the reveal tung bid rieng le hoac he thong tu dong reveal tat ca khi EndAuctionJob chay.

## Actors

- **Bidder (Nguoi mua):** Gui sealed bid (encrypted amount)
- **Admin:** Reveal tung sealed bid (tuy chon)
- **System:** Tu dong reveal tat ca va xac dinh winner khi ket thuc

## Endpoint Sequence

### Step 1: Gui Sealed Bid

- **Method:** `POST api/auctions/{auctionId}/sealed-bids`
- **Auth:** Required
- **Request:**
  ```json
  {
    "amountEncrypted": "base64-encoded-encrypted-amount"
  }
  ```
- **Response:** `200 OK` - `SealedBidDto`
  ```json
  {
    "id": "guid",
    "auctionId": "guid",
    "bidderId": "guid",
    "status": "submitted",
    "createdAt": "2026-04-01T15:00:00Z"
  }
  ```
- **Ghi chu:**
  - Chi ho tro cho auction type `Sealed`
  - Moi bidder chi duoc gui **mot** sealed bid per auction
  - Bid amount duoc ma hoa phia client

### Step 2: Admin Reveal (Tuy chon)

- **Method:** `POST api/admin/auctions/{auctionId}/sealed-bids/{sealedBidId}/reveal`
- **Auth:** Required (Admin)
- **Response:** `200 OK` - `SealedBidDto`
- **Ghi chu:**
  - Chi cho phep sau khi phien dau gia ket thuc (`HasEnded(nowUtc)`)
  - Danh dau sealed bid la `Revealed`

### Step 3: Tu dong Reveal khi Ket thuc (EndAuctionCommand)

Khi `EndAuctionCommand` xu ly sealed auction:
1. Load tat ca sealed bids
2. Decrypt moi bid bang `ISealedBidEncryptionService`
3. Truyen revealed amounts vao `AuctionGrain.EndAuctionAsync()`
4. Grain goi `auction.RevealAllSealedBids()` truoc khi `End()` va `Resolve()`

## Business Logic

### SubmitSealedBid Domain Logic

`auction.SubmitSealedBid(bidderId, amountEncrypted, nowUtc)`:

1. Kiem tra `AuctionType == Sealed`
2. `EnsureAcceptsBids(nowUtc)` - Auction phai Active
3. `EnsureNotSeller(bidderId)` - Khong phai seller
4. `EnsureBidderEligible(bidderId, nowUtc)` - Phai la qualified participant
5. Kiem tra chua co sealed bid nao tu bidder nay (status: Submitted) -> error `AlreadySubmitted`
6. Tao `SealedBid.Submit(auctionId, bidderId, amountEncrypted, nowUtc)`

### RevealAllSealedBids Domain Logic

`auction.RevealAllSealedBids(revealedBids, actorId, nowUtc)`:

1. Kiem tra `AuctionType == Sealed`
2. Kiem tra auction da het thoi gian (hoac status khong phai Active)
3. Reveal tat ca sealed bids (danh dau status = Revealed)
4. Neu da co Bids (tu reveal truoc do) -> tra ve sealed bids
5. Chuyen sealed bids thanh real Bids:
   - Loc bids co amount >= `StartingPrice`
   - Sap xep: amount DESC, createdAt ASC
   - Bid cao nhat: `Winning`, con lai: `Outbid`
6. Cap nhat Pricing, BidCount, PriceHistory

### Xac dinh Winner

```
RevealAllSealedBids()
    |
    v
Filter: amount >= startingPrice
    |
    v
Sort: amount DESC, createdAt ASC
    |
    v
[Co bid hop le?]
    |           |
  [Co]       [Khong]
    |           |
 Bid[0] =    (khong co winner)
 Winning       |
    |        End() -> Resolve() -> Failed
    |
 End() -> Resolve() -> Sold (neu reserve met)
                     -> Failed (neu reserve not met)
```

## SealedBid Status

```
Submitted ──[Reveal]──> Revealed
```

## Khac biet voi Regular Auction

| Tinh nang         | Regular            | Sealed                    |
|-------------------|--------------------|---------------------------|
| Live bidding      | Co                 | Khong                     |
| Auto-bid          | Co                 | Khong                     |
| Auto-extension    | Co (neu enabled)   | Khong bao gio             |
| Buy-now           | Co (khi Scheduled) | Co (khi Scheduled)        |
| Bid visibility    | Real-time          | Chi sau khi reveal        |
| Bid limit         | Khong gioi han     | 1 bid per bidder          |

## Luu y nghiep vu

- Sealed auction **khong ho tro** live bidding (`EnsureLiveBiddingSupported` -> error)
- Sealed auction **khong ho tro** auto-extension (`autoExtend` phai la `false`)
- Moi bidder chi gui duoc **mot** sealed bid - khong the cap nhat
- Bid amount duoc ma hoa phia client va giai ma phia server (ISealedBidEncryptionService)
- Khi co tie (cung amount), nguoi dat truoc (createdAt nho hon) thang
- Sealed bids co amount < `StartingPrice` bi loai khi materialized thanh Bids
- Admin co the reveal tung bid rieng le sau khi het gio (truoc khi EndAuctionJob chay)
