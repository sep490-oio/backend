# 16-03 -- Item Browsing

## Overview

A single anonymous endpoint returns full item details by ID. There is **no** public `GET /api/items` list endpoint -- items are discovered through auction listings (`GET /api/auctions`) or seller pages (`GET /api/sellers/{id}/items`).

---

## Endpoint

### Get Item by ID

| Property | Value |
|----------|-------|
| Route | `GET api/items/{itemId:guid}` |
| Auth | Anonymous |
| Handler | `GetItemByIdEndpoint` -> `GetItemByIdQueryHandler` |
| Response | `200 OK` -- `ItemDto` |

---

## ItemDto

```
ItemDto(
    Guid                      Id,
    Guid                      SellerId,
    Guid?                     CategoryId,
    string                    Title,
    string?                   Description,
    string                    Condition,       // e.g. "New", "LikeNew", "Good", "Fair"
    string                    Status,          // e.g. "Draft", "PendingReview", "Approved", "Active"
    int                       Quantity,
    IReadOnlyList<ItemMediaDto> Images,
    DateTime                  CreatedAt
)
```

### ItemMediaDto

```
ItemMediaDto(
    Guid    Id,
    string  Url,
    string  PublicId,
    string  ResourceType,
    bool    IsPrimary,
    int     SortOrder,
    string? FileName,
    long?   Bytes,
    string? Format,
    int?    Width,
    int?    Height,
    double? DurationSeconds
)
```

---

## Discovery Paths

Since there is no item list endpoint, clients reach individual items via:

| Discovery Path | Endpoint | How |
|----------------|----------|-----|
| Auction detail | `GET /api/auctions/{id}` | `AuctionDetailDto.Item` embeds the full `ItemDto` |
| Auction list | `GET /api/auctions` | Each `AuctionDto.ItemId` provides the ID for a follow-up fetch |
| Seller items | `GET /api/sellers/{id}/items` | Returns `PublicSellerItemDto[]` with item details |

---

## Related Concepts

| Concept | Description |
|---------|-------------|
| **Condition** | String enum describing physical state of the item |
| **Status** | Lifecycle state; only items in appropriate status appear in public results |
| **Images** | Ordered list via `SortOrder`; one marked `IsPrimary = true` for thumbnails |
| **CategoryId** | Links to the category tree (nullable for uncategorized items) |

---

## Source Files

| File | Path |
|------|------|
| Endpoint | `src/presentation/OIO.Api/Endpoints/AuctionContext/Items/GetItemByIdEndpoint.cs` |
| ItemDto | `src/core/OIO.Application/Context/AuctionContext/DTOs/ItemDto.cs` |
| ItemMediaDto | `src/core/OIO.Application/Context/AuctionContext/DTOs/ItemMediaDto.cs` |
