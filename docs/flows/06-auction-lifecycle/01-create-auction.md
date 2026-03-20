# 01 - Create Auction & Read Endpoints

## Overview

Auction creation uses an **all-in-one** endpoint that simultaneously creates an `Item` entity and an `Auction` aggregate in `Draft` status. The item and auction are linked via `Auction.ItemId`. Three read endpoints provide different views: detailed single-auction, public paginated list, and seller's own auctions.

**Source files:**

| Concern | Path |
|---|---|
| Command | `src/core/OIO.Application/Context/AuctionContext/Commands/CreateAuction/CreateAuctionCommand.cs` |
| Endpoint | `src/presentation/OIO.Api/Endpoints/AuctionContext/Auctions/CreateAuctionEndpoint.cs` |
| GetById query | `src/core/OIO.Application/Context/AuctionContext/Queries/GetAuctionById/GetAuctionByIdQuery.cs` |
| GetAuctions query | `src/core/OIO.Application/Context/AuctionContext/Queries/GetAuctions/GetAuctionsQuery.cs` |
| GetMyAuctions query | `src/core/OIO.Application/Context/AuctionContext/Queries/GetMyAuctions/GetMyAuctionsQuery.cs` |
| AuctionDto | `src/core/OIO.Application/Context/AuctionContext/DTOs/AuctionDto.cs` |
| AuctionListItemDto | `src/core/OIO.Application/Context/AuctionContext/DTOs/AuctionListItemDto.cs` |
| AuctionDetailDto | `src/core/OIO.Application/Context/AuctionContext/DTOs/AuctionDetailDto.cs` |

---

## Endpoints

### 1. POST `/api/auctions` -- Create Auction

**Permission:** `Catalogs.Auctions.Create`
**Response:** `201 Created` with `AuctionDto`

#### Request Body

```json
{
  "title": "string (required)",
  "condition": "string (required, one of ItemCondition.All)",
  "categoryId": "guid? (optional)",
  "description": "string? (optional)",
  "quantity": 1,
  "attributes": "string? (optional, JSON)",
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

#### Validation Rules (from `CreateAuctionCommand.Validate()`)

| Field | Rule |
|-------|------|
| `Title` | Not whitespace, max length = `App.Constraint.Item.TitleMaxLength` |
| `Condition` | Not whitespace, must be in `ItemCondition.All` |
| `CategoryId` | When provided: not empty GUID |
| `Description` | When provided: not whitespace |
| `Quantity` | Not default, positive |
| `Attributes` | When provided: not whitespace |
| `StartingPrice` | Non-negative |
| `BidIncrement` | Non-negative |
| `ReservePrice` | When provided: >= `StartingPrice` |
| `BuyNowPrice` | When provided: >= `StartingPrice` |
| `ExtensionMinutes` | Between 1 and 30 inclusive |
| `Currency` | Not whitespace, exactly 3 characters |
| `AuctionType` | Not whitespace, must be in `AuctionType.All` (`regular`, `sealed`) |

#### Handler Flow (`CreateAuctionCommandHandler`)

1. Validate category exists (if provided).
2. Load and validate media uploads: must exist, be owned by seller, be confirmed, not already linked.
3. Create `Item` entity via `Item.Create()`.
4. Attach media to item if provided.
5. Create auction via `AuctionDraftCreationService.CreateAsync()` -- pricing only, no timing at this stage.
6. Insert item + auction.
7. Relocate linked media uploads.
8. `SaveChangesAsync()`.
9. Return `AuctionDto`.

**Domain event raised:** `AuctionCreatedEvent` (from `Auction.Create()`).

---

### 2. GET `/api/auctions/{auctionId}` -- Get Auction by ID

**Auth:** Anonymous (public).
**Response:** `200 OK` with `AuctionDetailDto`.

`GetAuctionByIdQuery` validates that `AuctionId` is a non-empty GUID. Returns an `AuctionDetailDto` which wraps:

```
AuctionDetailDto
  - Auction: AuctionDto
  - Item: ItemDto
  - RecentBids: IReadOnlyList<BidDto>
  - PriceHistory: IReadOnlyList<PriceHistoryDto>
```

---

### 3. GET `/api/auctions` -- List Auctions

**Auth:** Anonymous (public).
**Response:** `200 OK` with `PagedList<AuctionListItemDto>`.

Accepts `GetAuctionsFilterParameters` as query string. Supports filtering by `Status` (must be valid `AuctionStatus.Id`), `CategoryId`, and `SortBy` (validated against `AuctionListItemDtoSortMapping`).

---

### 4. GET `/api/me/auctions` -- My Auctions

**Permission:** `Catalogs.Me.ReadAuctions`
**Response:** `200 OK` with `PagedList<AuctionListItemDto>`.

Accepts `GetMyAuctionsFilterParameters`. Filters by `Status` and `SortBy`. Scoped to the authenticated seller's auctions.

---

## AuctionDto Shape

Returned by create and update endpoints.

```
AuctionDto
  Id                  : Guid
  ItemId              : Guid
  SellerId            : Guid
  AuctionType         : string ("regular" | "sealed")
  StartingPrice       : MoneyDto
  ReservePrice        : MoneyDto?
  BuyNowPrice         : MoneyDto?
  CurrentPrice        : MoneyDto
  BidIncrement        : MoneyDto
  Currency            : string
  StartTime           : DateTime?
  EndTime             : DateTime?
  ActualEndTime       : DateTime?
  QualificationStartAt: DateTime?
  QualificationEndAt  : DateTime?
  Status              : string
  CurrentWinnerId     : Guid?
  AutoExtend          : bool
  ExtensionMinutes    : int
  ExtensionCount      : int
  AssignedAdminId     : Guid?
  AssignedAt          : DateTime?
  IsFeatured          : bool
  Priority            : decimal
  PriorityReason      : string
  VerifyByPlatform    : bool
  RejectionCount      : int
  ViewCount           : int
  BidCount            : int
  WatchCount          : int
  MinimumBidAmount    : MoneyDto
  IsReserveMet        : bool
  HasBuyNow           : bool
  IsBuyNowReserved    : bool
  BuyNowReservedUntil : DateTime?
  RemainingTime       : TimeSpan
  IsEndingSoon        : bool
  CreatedAt           : DateTime
```

## AuctionListItemDto Shape

Returned by list endpoints.

```
AuctionListItemDto
  Id                  : Guid
  ItemTitle           : string
  PrimaryImageUrl     : string?
  CurrentPrice        : MoneyDto
  StartingPrice       : MoneyDto
  BuyNowPrice         : MoneyDto?
  IsBuyNowReserved    : bool
  BuyNowReservedUntil : DateTime?
  Currency            : string
  Status              : string
  BidCount            : int
  WatchCount          : int
  StartTime           : DateTime?
  EndTime             : DateTime?
  RemainingTime       : TimeSpan?
  IsEndingSoon        : bool?
  IsFeatured          : bool?
  SellerId            : Guid
```

---

## Error Codes

| Code | When |
|------|------|
| `Auction.NotFound` | AuctionId does not exist |
| `Category.NotFound` | CategoryId does not exist |
| `Media.NotFounds` | One or more media upload IDs not found |
| `Media.NotOwnedByUser` | Media upload not owned by current user |
| `Media.NotConfirm` | Media upload not confirmed |
| `Media.AlreadyLinked` | Media upload already linked to another entity |
| `Auction.InvalidAuctionType` | AuctionType not in `regular` / `sealed` |
| `Auction.ItemAlreadyInAuction` | Item is already in an active auction |
| `Auction.ItemAlreadyHasAuction` | Item already has a commercial-state auction |
| `Auction.ItemRequiresMedia` | Item must have at least one image |
