# 17-02 -- My Bids

## Endpoint

| Property | Value |
|----------|-------|
| Route | `GET /api/me/bids` |
| Permission | `me:bids:read` |
| Tag | `Me` |
| Response | `PagedList<MyBidDto>` |

---

## Filter Parameters

Inherits from `PagedParameters` (default page 1, size 10, max 50).

| Param | Type | Description |
|-------|------|-------------|
| `status` | `string?` | Filter by `BidStatus`. Must be one of the known status values (see below). |
| `sortBy` | `string?` | Sort expression validated against `BidMappings.MyBidDtoSortMapping`. |
| `pageNumber` | `int?` | Page number (default 1). |
| `pageSize` | `int?` | Page size (default 10, max 50). |

### Allowed `status` values

`active`, `outbid`, `winning`, `won`, `cancelled`

---

## Sort Options

Sort mapping keys from `BidMappings.MyBidDtoSortMapping`:

| Sort Key | Maps To |
|----------|---------|
| `id` | `Bid.Id` |
| `auctionId` | `Bid.AuctionId` |
| `itemTitle` | `Bid.Auction.Item.Title` |
| `amount` | `Bid.Amount.Amount` |
| `status` | `Bid.Status.Id` |
| `currentPrice` | `Bid.Auction.Pricing.CurrentAmount` |
| `auctionStatus` | `Bid.Auction.Status` |
| `bidPlacedAt` | `Bid.CreatedAt` |
| `auctionEndTime` | `Bid.Auction.Info.EndTime` |

---

## MyBidDto (11 fields)

| Field | Type | Description |
|-------|------|-------------|
| `id` | `Guid` | Bid identifier |
| `auctionId` | `Guid` | Associated auction ID |
| `itemTitle` | `string` | Title of the auctioned item |
| `primaryImageUrl` | `string?` | URL of the primary media image |
| `amount` | `MoneyDto` | The bid amount (`{ amount, currency, symbol }`) |
| `currentPrice` | `MoneyDto` | Current highest bid price on the auction |
| `status` | `string` | Bid status (`active`, `outbid`, `winning`, `won`, `cancelled`) |
| `auctionStatus` | `string` | Current status of the auction |
| `isHighestBid` | `bool` | `true` if this bid is currently the highest bid on the auction |
| `bidPlacedAt` | `DateTime` | Timestamp when the bid was placed |
| `auctionEndTime` | `DateTime?` | When the auction is scheduled to end |

### IsHighestBid Flag

The `isHighestBid` flag indicates whether this particular bid is currently the leading bid. This is distinct from `status: winning` -- a bid could have status `active` (not yet evaluated as winning) while still being the highest. The flag helps the UI display "You are the highest bidder" badges.

---

## Source References

| File | Path |
|------|------|
| Endpoint | `src/presentation/OIO.Api/Endpoints/UserContext/Me/GetMyBidsEndpoint.cs` |
| Query | `src/core/OIO.Application/Context/AuctionContext/Queries/GetMyBids/GetMyBidsQuery.cs` |
| Filter | `src/core/OIO.Application/Context/AuctionContext/Queries/GetMyBids/GetMyBidsFilterParameters.cs` |
| DTO | `src/core/OIO.Application/Context/AuctionContext/DTOs/MyBidDto.cs` |
| Mappings | `src/core/OIO.Application/Context/AuctionContext/Mappings/BidMappings.cs` |
