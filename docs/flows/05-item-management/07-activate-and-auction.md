# 07 - Activate Item and Create Auction

## Overview

Once an item reaches `approved` status (either through admin review or warehouse inspection),
the seller can activate it and create a draft auction.
Activation transitions the item to `active`, signaling it is ready for listing.
Creating an auction from the item produces a draft `Auction` entity linked to the item.

---

## Endpoints

### 1. POST `/api/items/{itemId}/activate`

Activate an approved item. The seller must own the item and it must have at least one media attachment.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `itemId` | path | `Guid` | Yes |

No request body.

**Authorization:** `Catalogs.Items.Activate`

**Response:** `204 No Content`

**Preconditions:**
- Caller must be the item's seller (`item.SellerId == currentUser`).
- Item must have at least one media attachment.
- Item status must support transition to `active`. Valid source statuses per `CanTransitionTo`: `draft`, `approved`.

Side-effects:
- Status transitions: `approved` -> `active` (also `draft` -> `active` is structurally allowed by `CanTransitionTo`).
- Raises `ItemStatusChangedEvent`.

**Error responses:**

| HTTP | Code | Condition |
|---|---|---|
| 404 | `Item.NotFound` | Item does not exist |
| 403 | `Item.NotOwnedByUser` | Caller is not the seller |
| 409 | `Item.InvalidState` | Status cannot transition to `active` |
| 409 | `Item.CannotActivate` | Item has zero media attachments |

---

### 2. POST `/api/items/{itemId}/auctions`

Create a draft auction linked to the item.

| Parameter | Location | Type | Required | Validation | Default |
|---|---|---|---|---|---|
| `itemId` | path | `Guid` | Yes | Non-empty GUID | |
| `startingPrice` | body | `decimal` | Yes | Non-negative | `0` |
| `bidIncrement` | body | `decimal` | Yes | Non-negative | `0` |
| `reservePrice` | body | `decimal?` | No | >= `startingPrice` when provided | `null` |
| `buyNowPrice` | body | `decimal?` | No | >= `startingPrice` when provided | `null` |
| `extensionMinutes` | body | `int` | No | Between 1 and 30 inclusive | `5` |
| `currency` | body | `string` | No | Exactly 3 characters, not whitespace | `"VND"` |
| `auctionType` | body | `string` | No | One of: `regular`, `sealed` | `"regular"` |

**Authorization:** `Catalogs.Auctions.Create`

**Response:** `201 Created` — `AuctionDto`

**Preconditions (enforced by `AuctionDraftCreationService`):**
- Caller must be the item's seller.
- Item must have at least one media attachment.
- Item status must be one of: `draft`, `pending_review`, `pending_verify`, `pending_condition_confirmation`, `approved`, `active`.
- No existing auction in a blocking status (`draft`, `pending`, `approved`, `scheduled`, `active`, `ended`, `sold`).
- If a `payment_defaulted` auction exists, seller must use the relist endpoint instead.

**Pricing validation rules:**
- `startingPrice` must be non-negative.
- `bidIncrement` must be non-negative.
- `reservePrice` (when provided) must be >= `startingPrice`.
- `buyNowPrice` (when provided) must be >= `startingPrice`.
- `currency` must be a supported currency code (validated via `Currency.FromId`).
- `auctionType` must be one of the values in `AuctionType.All` (`regular`, `sealed`).

---

### 3. GET `/api/items/{itemId}`

Standard item detail endpoint to verify status after activation or auction creation.

| Parameter | Location | Type | Required |
|---|---|---|---|
| `itemId` | path | `Guid` | Yes |

**Response:** `200 OK` — `ItemDto`

---

## Response DTOs

### AuctionDto

| Field | Type | Description |
|---|---|---|
| `id` | `Guid` | Auction identifier |
| `itemId` | `Guid` | Linked item ID |
| `sellerId` | `Guid` | Seller user ID |
| `auctionType` | `string` | `regular` or `sealed` |
| `startingPrice` | `MoneyDto` | Starting bid price (`{ amount, currency, symbol }`) |
| `reservePrice` | `MoneyDto?` | Minimum price for the item to sell (null if not set) |
| `buyNowPrice` | `MoneyDto?` | Instant purchase price (null if not set) |
| `currentPrice` | `MoneyDto` | Current highest bid or starting price |
| `bidIncrement` | `MoneyDto` | Minimum bid increment |
| `currency` | `string` | 3-character currency code |
| `startTime` | `DateTime?` | Scheduled start (null for draft) |
| `endTime` | `DateTime?` | Scheduled end (null for draft) |
| `actualEndTime` | `DateTime?` | Actual end time (may differ due to extensions) |
| `qualificationStartAt` | `DateTime?` | Qualification window start |
| `qualificationEndAt` | `DateTime?` | Qualification window end |
| `status` | `string` | Auction status (e.g. `draft`) |
| `currentWinnerId` | `Guid?` | Current highest bidder |
| `autoExtend` | `bool` | Whether anti-sniping auto-extension is enabled |
| `extensionMinutes` | `int` | Minutes added per extension (1-30) |
| `extensionCount` | `int` | Number of extensions applied so far |
| `assignedAdminId` | `Guid?` | Admin assigned for review |
| `assignedAt` | `DateTime?` | When admin was assigned |
| `isFeatured` | `bool` | Whether the auction is featured |
| `priority` | `decimal` | Curation priority score |
| `priorityReason` | `string` | Reason for priority assignment |
| `verifyByPlatform` | `bool` | Whether item requires platform verification |
| `rejectionCount` | `int` | Number of times auction was rejected |
| `viewCount` | `int` | Total views |
| `bidCount` | `int` | Total bids placed |
| `watchCount` | `int` | Number of watchers |
| `minimumBidAmount` | `MoneyDto` | Minimum amount for next bid |
| `isReserveMet` | `bool` | Whether the reserve price has been met |
| `hasBuyNow` | `bool` | Whether buy-now is available |
| `isBuyNowReserved` | `bool` | Whether a buy-now reservation is active |
| `buyNowReservedUntil` | `DateTime?` | Buy-now reservation expiry |
| `remainingTime` | `TimeSpan` | Time remaining until auction ends |
| `isEndingSoon` | `bool` | Whether the auction is within the extension threshold |
| `createdAt` | `DateTime` | Auction creation timestamp |

### MoneyDto

| Field | Type | Description |
|---|---|---|
| `amount` | `decimal` | Monetary amount |
| `currency` | `string` | Currency code (e.g. `VND`) |
| `symbol` | `string` | Currency symbol |

---

## Error Codes

| Code | HTTP | Trigger |
|---|---|---|
| `Item.NotFound` | 404 | Item does not exist |
| `Item.NotOwnedByUser` | 403 | Caller is not the item's seller |
| `Item.InvalidState` | 409 | Item status does not allow activation |
| `Item.CannotActivate` | 409 | Item has no media attachments |
| `Item.NotAvailable` | 409 | Item status not in allowed set for auction creation |
| `Auction.OnlyOwnerOfItem` | 403 | Only the item's seller can create an auction |
| `Auction.ItemRequiresMedia` | 409 | Item has no media — cannot create auction |
| `Auction.ItemAlreadyHasAuction` | 409 | Item already has an auction in a blocking status |
| `Auction.PaymentDefaultedRequiresRelist` | 409 | A payment-defaulted auction exists; must use relist |
| `Auction.InvalidStartingPrice` | 422 | Starting price is negative |
| `Auction.InvalidIncrement` | 422 | Bid increment is not positive |
| `Auction.InvalidReserve` | 422 | Reserve price is below starting price |
| `Auction.InvalidBuyNow` | 422 | Buy-now price is below starting price |
| `Auction.InvalidAuctionType` | 422 | Auction type not in supported set |
