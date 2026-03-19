# Create Auction

## Tong quan

Co hai cach tao phien dau gia:
1. **Tao truc tiep (all-in-one):** Tao Item + Auction cung luc qua `POST api/auctions`
2. **Tao tu Item da ton tai:** Tao Auction tu mot Item da duoc duyet qua `POST api/items/{itemId}/auctions`

Ca hai cach deu tao Auction o trang thai `Draft` voi pricing (khong co timing). Timing duoc thiet lap rieng qua `SetTiming` hoac `UpdateAuction`.

## Actors

- **Seller (Nguoi ban):** Nguoi tao phien dau gia

## Endpoint Sequence

### Cach 1: Tao truc tiep (All-in-One)

#### Step 1: Tao Auction + Item
- **Method:** `POST api/auctions`
- **Auth:** Required (Seller)
- **Request:**
  ```json
  {
    "title": "string (bat buoc)",
    "condition": "string (bat buoc, vi du: 'new', 'like_new', ...)",
    "categoryId": "guid? (tuy chon)",
    "description": "string? (tuy chon)",
    "quantity": 1,
    "attributes": "string? (JSON tuy chon)",
    "media": [
      {
        "mediaUploadId": "guid",
        "isPrimary": true,
        "sortOrder": 0
      }
    ],
    "startingPrice": 0,
    "bidIncrement": 0,
    "reservePrice": null,
    "buyNowPrice": null,
    "extensionMinutes": 5,
    "currency": "VND",
    "auctionType": "regular"
  }
  ```
- **Response:** `200 OK` - `AuctionDto`
- **Ghi chu:**
  - Tao Item truoc, sau do tao Auction
  - AuctionType hop le: `regular`, `sealed`
  - ExtensionMinutes: 1-30
  - Neu co `media`, he thong validate: upload phai ton tai, chua bi link, da confirm, thuoc so huu cua seller
  - Auction duoc tao o trang thai `Draft`
  - Gia khoi diem (`startingPrice`) phai >= 0
  - Gia dat mua ngay (`buyNowPrice`) phai >= `startingPrice` (neu co)
  - Gia san (`reservePrice`) phai >= `startingPrice` (neu co)

### Cach 2: Tao tu Item da ton tai

#### Step 1: Tao Auction tu Item
- **Method:** `POST api/items/{itemId}/auctions`
- **Auth:** Required (Seller, phai la owner cua Item)
- **Request:**
  ```json
  {
    "startingPrice": 0,
    "bidIncrement": 0,
    "reservePrice": null,
    "buyNowPrice": null,
    "extensionMinutes": 5,
    "currency": "VND",
    "auctionType": "regular"
  }
  ```
- **Response:** `200 OK` - `AuctionDto`
- **Ghi chu:**
  - Item phai ton tai va thuoc so huu cua seller hien tai
  - Item phai o trang thai cho phep tao auction
  - Su dung `AuctionDraftCreationService` de tao Auction

## Business Logic (AuctionDraftCreationService)

1. Validate quyen so huu Item
2. Tao `AuctionPricing` tu cac tham so gia
3. Goi `Auction.Create(sellerId, itemId, auctionType, pricing, nowUtc)`
4. Auction duoc tao voi trang thai `Draft`
5. Tu dong tao `AuctionPriceHistory` (starting price)
6. Raise `AuctionCreatedEvent`

## Domain Events

| Event                | Payload                                              |
|----------------------|------------------------------------------------------|
| `AuctionCreatedEvent`| AuctionId, ItemId, SellerId, StartingPrice, StartTime, EndTime |

## Luu y nghiep vu

- Auction o trang thai `Draft` chua the nhan bid, chua co lich trinh
- Phai thuc hien `SubmitConfiguration` (va co the can `SetTiming`) truoc khi co the publish
- Mot Item co the co nhieu Auction (vi du khi relist), nhung chi co mot Auction active tai mot thoi diem
- AuctionType `sealed` khong ho tro auto-extend
