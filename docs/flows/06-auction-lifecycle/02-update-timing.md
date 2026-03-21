# 02 - Update Auction & Set Timing

## Overview

Two endpoints allow sellers to modify auction configuration after creation:

1. **`PUT /api/auctions/{auctionId}`** -- General update of pricing, type, and optionally timing. Allowed in Draft, Approved, and Scheduled states (with restrictions).
2. **`PUT /api/auctions/{auctionId}/timing`** -- Dedicated timing endpoint, only available when status is `Approved`. Transitions the auction to `Scheduled`.

> **Prerequisite:** The `SetAuctionTiming` endpoint requires the auction to already be in `Approved` status. This means the auction must first be submitted via `POST /api/auctions/{id}/submit` (which itself requires the item to be `Approved`). You cannot set timing on a `Draft` auction -- submit it first.

**Source files:**

| Concern | Path |
|---|---|
| UpdateAuctionCommand | `src/core/OIO.Application/Context/AuctionContext/Commands/UpdateAuction/UpdateAuctionCommand.cs` |
| SetAuctionTimingCommand | `src/core/OIO.Application/Context/AuctionContext/Commands/SetAuctionTiming/SetAuctionTimingCommand.cs` |
| UpdateAuctionEndpoint | `src/presentation/OIO.Api/Endpoints/AuctionContext/Auctions/UpdateAuctionEndpoint.cs` |
| SetAuctionTimingEndpoint | `src/presentation/OIO.Api/Endpoints/AuctionContext/Auctions/SetAuctionTimingEndpoint.cs` |
| Auction domain | `src/core/OIO.Domain/Context/AuctionContext/Aggregates/Auctions/Auction.cs` -- `UpdateConfiguration()`, `SetTiming()` |

---

## Endpoint 1: PUT `/api/auctions/{auctionId}` -- Update Auction

**Permission:** `Catalogs.Auctions.Create`
**Response:** `200 OK` with `AuctionDto`

### Request Body

All fields are optional -- only provided fields are updated.

```json
{
  "startingPrice": 100000,
  "bidIncrement": 10000,
  "reservePrice": 500000,
  "buyNowPrice": 1000000,
  "currency": "VND",
  "auctionType": "regular",
  "startTime": "2026-04-01T10:00:00Z",
  "endTime": "2026-04-02T10:00:00Z",
  "qualificationStartAt": "2026-03-30T10:00:00Z",
  "qualificationEndAt": "2026-04-01T09:00:00Z",
  "autoExtend": true,
  "extensionMinutes": 5
}
```

### Validation Rules (from `UpdateAuctionCommand.Validate()`)

| Field | Rule |
|-------|------|
| `AuctionId` | Not empty GUID |
| `StartingPrice` | When provided: non-negative |
| `BidIncrement` | When provided: positive |
| `ReservePrice` | When provided: non-negative |
| `BuyNowPrice` | When provided: positive |
| `Currency` | When provided: exactly 3 characters |
| `AuctionType` | When provided: must be in `AuctionType.All` |
| `ExtensionMinutes` | When provided: between 1 and 30 inclusive |

### Handler Flow (`UpdateAuctionCommandHandler`)

1. Load auction with Item.
2. Verify current user is seller (owner of item).
3. Resolve `AuctionType` -- use request value, or fallback to existing, or default to `regular`.
4. **Sealed auction restriction:** If type is `sealed` and `autoExtend` is `true`, return error `Auction.SealedAutoExtendNotSupported`.
5. Build `AuctionPricing` from request values merged with existing values.
6. Build `AuctionInfo` (timing) if any timing-related field is present:
   - `StartTime` and `EndTime` must be provided together.
   - `QualificationStartAt` and `QualificationEndAt` are required when timing exists.
   - Creates a `QualificationWindow` value object.
7. Call `Auction.UpdateConfiguration()`:
   - **Blocked states:** Active, Ended, Sold, PaymentDefaulted, Failed, Cancelled, Terminated -> `Auction.CannotEdit`.
   - **Has bids:** `BidCount > 0` -> `Auction.CannotEdit`.
   - **Scheduled without timing:** If status is Scheduled and `info` is null -> `Auction.TimingRequired`.
   - If status is `Approved` and info is provided -> auto-transition to `Scheduled`.
8. Save changes.
9. If now `Scheduled` with timing, schedule start job via `IAuctionScheduler.ScheduleStartAsync()`.

---

## Endpoint 2: PUT `/api/auctions/{auctionId}/timing` -- Set Timing

**Permission:** `Catalogs.Auctions.Create`
**Response:** `200 OK` with `AuctionDto`

### Request Body

All fields are required.

```json
{
  "startTime": "2026-04-01T10:00:00Z",
  "endTime": "2026-04-02T10:00:00Z",
  "qualificationStartAt": "2026-03-30T10:00:00Z",
  "qualificationEndAt": "2026-04-01T09:00:00Z",
  "autoExtend": true,
  "extensionMinutes": 5
}
```

### Validation Rules (from `SetAuctionTimingCommand.Validate()`)

| Field | Rule |
|-------|------|
| `AuctionId` | Not empty GUID |
| `QualificationStartAt` | Not in the past |
| `QualificationEndAt` | Not in the past |
| `ExtensionMinutes` | Between 1 and 30 inclusive |

### Handler Flow (`SetAuctionTimingCommandHandler`)

1. Load auction with Item and Media.
2. Verify current user is seller.
3. **Status guard:** Must be `Approved`. Otherwise -> `Auction.CannotSetTiming`.
4. **Sealed auction restriction:** If `AuctionType == Sealed` and `autoExtend == true` -> error `Auction.SealedAutoExtendNotSupported`.
5. Create `QualificationWindow` from start/end times.
6. Create `AuctionInfo` via `AuctionInfo.Create()`.
7. Call `Auction.SetTiming()`:
   - Verifies status is `Approved`.
   - Verifies can transition to `Scheduled`.
   - Sets `Info`, transitions status to `Scheduled`.
   - Raises `AuctionScheduledEvent`.
8. Save changes.
9. Return updated `AuctionDto`.

---

## Qualification Window Concept

The `QualificationWindow` is a value object that defines the period during which bidders can register as participants and place deposits to become eligible for the auction. Fields:

- `StartTime` -- when registration opens
- `EndTime` -- when registration closes (typically before auction `StartTime`)

`AuctionInfo.HasQualification` returns `true` when the qualification window is set. This is **required** for activation -- `AuctionActivationService` checks `HasQualification` and returns `Auction.QualificationWindowRequired` if missing.

---

## Error Codes

| Code | When |
|------|------|
| `Auction.NotFound` | Auction ID does not exist |
| `Auction.OnlyOwnerOfItem` | Current user is not the seller |
| `Auction.CannotEdit` | Auction is in a non-editable state or has bids |
| `Auction.CannotSetTiming` | Auction is not in `Approved` status |
| `Auction.TimingRequired` | Scheduled auction missing timing info |
| `Auction.SealedAutoExtendNotSupported` | Sealed auction with `autoExtend=true` |
| `Auction.InvalidAuctionType` | Unknown auction type |
| `Auction.InvalidPeriod` | EndTime not later than StartTime |
| `Auction.QualificationWindowRequired` | Qualification window missing |
| `Currency.NotSupported` | Unknown currency code |
