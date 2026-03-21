# 17-06 -- My Auto-Bid

## Endpoint

| Property | Value |
|----------|-------|
| Route | `GET /api/auctions/{auctionId}/auto-bid/my` |
| Permission | (authenticated -- no specific permission) |
| Tag | `Auctions` |
| Response | `AutoBidDto?` (nullable) |
| Error | `404 Not Found` |

---

## Key Characteristics

- **Per-auction scope** -- this is NOT a global list of all auto-bids. You must supply the `auctionId` in the URL to retrieve the current user's auto-bid configuration for that specific auction.
- **Returns null** if the user has no auto-bid configured for the given auction.
- No filter parameters, no pagination.

---

## AutoBidDto (15 fields)

| Field | Type | Description |
|-------|------|-------------|
| `id` | `Guid` | Auto-bid configuration ID |
| `auctionId` | `Guid` | Associated auction ID |
| `bidderId` | `Guid` | The user who set up the auto-bid |
| `isEnabled` | `bool` | Whether the auto-bid is currently active |
| `maxAmount` | `MoneyDto` | Maximum budget for auto-bidding |
| `currentAmount` | `MoneyDto` | Amount spent so far via auto-bids |
| `remainingBudget` | `MoneyDto` | Budget remaining (`maxAmount - currentAmount`) |
| `incrementAmount` | `MoneyDto?` | Custom bid increment (null = use auction default) |
| `status` | `string` | Current auto-bid status (see below) |
| `totalAutoBids` | `int` | Number of auto-bids placed |
| `lastAutoBidAt` | `DateTime?` | When the last auto-bid was placed |
| `stopReason` | `string?` | Why the auto-bid was stopped (see below) |
| `stoppedAt` | `DateTime?` | When the auto-bid was stopped |
| `lastValidationAt` | `DateTime?` | When the auto-bid was last validated |
| `createdAt` | `DateTime` | When the auto-bid was configured |

---

## Status Values

From `AutoBidStatus`:

| Status | Description |
|--------|-------------|
| `active` | Auto-bid is running and will place bids when outbid |
| `paused` | Temporarily paused by user |
| `exhausted` | Budget fully consumed |
| `won` | The auction was won |
| `outbid` | Outbid beyond max amount |

---

## StopReason Values

The `stopReason` is a free-form string set when the auto-bid is disabled:

| Reason | Trigger |
|--------|---------|
| `paused_by_user` | User manually paused the auto-bid |
| `won` | Auction ended and user won |
| `outbid` | Another bidder exceeded the max amount |
| `budget_exhausted` | Remaining budget insufficient for next bid |
| `null` | Auto-bid is active (not stopped) |

---

## Source References

| File | Path |
|------|------|
| Endpoint | `src/presentation/OIO.Api/Endpoints/AuctionContext/Auctions/GetMyAutoBidEndpoint.cs` |
| Query | `src/core/OIO.Application/Context/AuctionContext/Queries/GetMyAutoBid/GetMyAutoBidQuery.cs` |
| DTO | `src/core/OIO.Application/Context/AuctionContext/DTOs/AutoBidDto.cs` |
| Status Enum | `src/core/OIO.Domain/Context/AuctionContext/Enums/AutoBidStatus.cs` |
| Entity | `src/core/OIO.Domain/Context/AuctionContext/Aggregates/Auctions/AutoBid.cs` |
