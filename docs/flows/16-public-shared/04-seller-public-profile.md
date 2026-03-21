# 16-04 -- Seller Public Profile

## Overview

Two anonymous endpoints expose seller storefronts: a profile summary with trust score, and a paginated item listing that includes live auction data.

---

## Endpoints

### 1. Get Seller Profile

| Property | Value |
|----------|-------|
| Route | `GET api/sellers/{sellerId:guid}` |
| Auth | Anonymous |
| Handler | `GetSellerByIdEndpoint` -> `GetPublicSellerProfileQueryHandler` |
| Response | `200 OK` -- `PublicSellerProfileDto` |

### 2. Get Seller Items

| Property | Value |
|----------|-------|
| Route | `GET api/sellers/{sellerId:guid}/items` |
| Auth | Anonymous |
| Handler | `GetSellerItemsEndpoint` -> `GetPublicSellerItemsQueryHandler` |
| Response | `200 OK` -- Paged `PublicSellerItemDto[]` |

**Query Parameters:**

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| `pageNumber` | int? | 1 | Page number (min 1) |
| `pageSize` | int? | 10 | Page size (max 50) |

---

## DTOs

### PublicSellerProfileDto

```
PublicSellerProfileDto(
    Guid     Id,
    string   StoreName,
    string   StoreDescription,
    string   Status,             // e.g. "Active", "Verified"
    int      TotalSalesCount,
    decimal  TrustScore,         // 0-100 weighted composite score
    DateTime CreatedAt
)
```

> Note: The internal `SellerProfileDto` includes additional fields (`TotalSalesAmount`, `VerifiedAt`, `TrustScoreCalculatedAt`, `ModifiedAt`) that are **not** exposed in the public DTO.

### PublicSellerItemDto

```
PublicSellerItemDto(
    Guid                           Id,
    Guid                           SellerId,
    Guid?                          CategoryId,
    string                         Title,
    string?                        Description,
    string                         Condition,
    string                         Status,
    int                            Quantity,
    IReadOnlyList<ItemMediaDto>    Images,
    DateTime                       CreatedAt,
    PublicSellerItemAuctionSummaryDto? Auction,
    bool                           HasLiveAuction
)
```

### PublicSellerItemAuctionSummaryDto

```
PublicSellerItemAuctionSummaryDto(
    Guid      AuctionId,
    string    AuctionStatus,
    string    AuctionType,
    decimal   CurrentPrice,
    string    Currency,
    DateTime? StartTime,
    DateTime? EndTime
)
```

---

## TrustScore Calculation

The `SellerTrustScoreCalculator` computes a composite score (0--100) from five weighted components:

| # | Component | Weight | Formula |
|---|-----------|--------|---------|
| 1 | **Rating** | 30% | `AverageRating / 5.0 * 100` (from `SellerRatingSummary`) |
| 2 | **Order Completion** | 25% | `CompletedOrders / TotalOrders * 100` |
| 3 | **Dispute** | 20% | `(1 - OpenDisputes / TotalOrders) * 100` |
| 4 | **Verification** | 15% | Approved = 100; AutoVerifyScore if available; else 50 |
| 5 | **Risk** | 10% | Based on highest risk flag severity: None=100, Low=80, Medium=50, High=20, Critical=0 |

**Default score** for sellers with no data in a component: **50**.

Final score: `Math.Clamp(weighted_sum, 0, 100)`

Recalculated periodically by `RecalculateSellerTrustScoresJob`.

---

## Source Files

| File | Path |
|------|------|
| Endpoint (profile) | `src/presentation/OIO.Api/Endpoints/UserContext/Sellers/GetSellerByIdEndpoint.cs` |
| Endpoint (items) | `src/presentation/OIO.Api/Endpoints/UserContext/Sellers/GetSellerItemsEndpoint.cs` |
| PublicSellerProfileDto | `src/core/OIO.Application/Context/UserContext/DTOs/SellerProfileDto.cs` |
| PublicSellerItemDto | `src/core/OIO.Application/Context/AuctionContext/DTOs/PublicSellerItemDto.cs` |
| TrustScore calculator | `src/core/OIO.Application/Context/UserContext/Services/SellerTrustScoreCalculator.cs` |
| Background job | `src/infrastructure/OIO.Infrastructure/Scheduling/Jobs/RecalculateSellerTrustScoresJob.cs` |
