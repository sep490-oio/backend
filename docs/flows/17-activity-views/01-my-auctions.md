# 17-01 -- My Auctions

## Endpoint

| Property | Value |
|----------|-------|
| Route | `GET /api/me/auctions` |
| Permission | `me:auctions:read` |
| Tag | `Me` |
| Response | `PagedList<AuctionListItemDto>` |

---

## Filter Parameters

Inherits from `PagedParameters` (default page 1, size 10, max 50).

| Param | Type | Description |
|-------|------|-------------|
| `status` | `string?` | Filter by `AuctionStatus`. Must be one of the known status values (see below). |
| `sortBy` | `string?` | Sort expression validated against `AuctionListItemDtoSortMapping`. |
| `pageNumber` | `int?` | Page number (default 1). |
| `pageSize` | `int?` | Page size (default 10, max 50). |

### Allowed `status` values

`draft`, `pending`, `approved`, `scheduled`, `active`, `ended`, `sold`, `payment_defaulted`, `cancelled`, `failed`, `terminated`

---

## Sort Options

Sort mapping keys from `AuctionMappings.AuctionListItemDtoSortMapping`:

| Sort Key | Maps To |
|----------|---------|
| `id` | `Auction.Id` |
| `itemTitle` | `Auction.Item.Id` |
| `currentPrice` | `Auction.Pricing.CurrentAmount` |
| `startingPrice` | `Auction.Pricing.StartingAmount` |
| `buyNowPrice` | `Auction.Pricing.BuyNowAmount` |
| `status` | `Auction.Status.Id` |
| `bidCount` | `Auction.BidCount` |
| `watchCount` | `Auction.WatchCount` |
| `startTime` | `Auction.Info.StartTime` |
| `endTime` | `Auction.Info.EndTime` |
| `isFeatured` | `Auction.IsFeatured` |
| `sellerId` | `Auction.Item.SellerId` |

Use `+key` for ascending, `-key` for descending (e.g. `-currentPrice`).

---

## AuctionListItemDto (18 fields)

| Field | Type | Description |
|-------|------|-------------|
| `id` | `Guid` | Auction identifier |
| `itemTitle` | `string` | Title of the auctioned item |
| `primaryImageUrl` | `string?` | URL of the primary media image |
| `currentPrice` | `MoneyDto` | Current highest bid price (`{ amount, currency, symbol }`) |
| `startingPrice` | `MoneyDto` | Starting price |
| `buyNowPrice` | `MoneyDto?` | Buy-now price (null if not set) |
| `isBuyNowReserved` | `bool` | Whether a buy-now reservation is active |
| `buyNowReservedUntil` | `DateTime?` | Reservation expiry timestamp |
| `currency` | `string` | Currency code (e.g. `VND`) |
| `status` | `string` | Current auction status |
| `bidCount` | `int` | Total number of bids |
| `watchCount` | `int` | Total number of watchers |
| `startTime` | `DateTime?` | Scheduled start time |
| `endTime` | `DateTime?` | Scheduled end time |
| `remainingTime` | `TimeSpan?` | Time left until auction ends |
| `isEndingSoon` | `bool?` | True if within the extension threshold window |
| `isFeatured` | `bool?` | Whether the auction is featured |
| `sellerId` | `Guid` | Seller's user ID |

### MoneyDto

```
{ amount: decimal, currency: string, symbol: string }
```

---

## Source References

| File | Path |
|------|------|
| Endpoint | `src/presentation/OIO.Api/Endpoints/UserContext/Me/GetMyAuctionsEndpoint.cs` |
| Query | `src/core/OIO.Application/Context/AuctionContext/Queries/GetMyAuctions/GetMyAuctionsQuery.cs` |
| Filter | `src/core/OIO.Application/Context/AuctionContext/Queries/GetMyAuctions/GetMyAuctionsFilterParameters.cs` |
| DTO | `src/core/OIO.Application/Context/AuctionContext/DTOs/AuctionListItemDto.cs` |
| Mappings | `src/core/OIO.Application/Context/AuctionContext/Mappings/AuctionMappings.cs` |
