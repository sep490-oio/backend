# 17-04 -- My Items

## Endpoint

| Property | Value |
|----------|-------|
| Route | `GET /api/items/my` |
| Permission | (authenticated -- no specific permission) |
| Tag | `Items` |
| Response | `PagedList<ItemDto>` |

> **Note:** This endpoint uses the route `/api/items/my` (under the Items tag), not `/api/me/items` like other "me" endpoints. This is because items belong to the `AuctionContext` Items group rather than the `UserContext` Me group.

---

## Filter Parameters

Inherits from `PagedParameters` (default page 1, size 10, max 50).

| Param | Type | Description |
|-------|------|-------------|
| `sortBy` | `string?` | Sort expression validated against `ItemMappings.ItemDtoSortMapping` |
| `pageNumber` | `int?` | Page number (default 1) |
| `pageSize` | `int?` | Page size (default 10, max 50) |

No status filter is available -- all items for the current seller are returned.

---

## Sort Options

Sort mapping keys from `ItemMappings.ItemDtoSortMapping`:

| Sort Key | Maps To |
|----------|---------|
| `id` | `Item.Id` |
| `sellerId` | `Item.SellerId` |
| `categoryId` | `Item.CategoryId` |
| `title` | `Item.Title` |
| `condition` | `Item.Condition.Id` |
| `status` | `Item.Status.Id` |
| `quantity` | `Item.Quantity` |
| `createdAt` | `Item.CreatedAt` |

---

## ItemDto (10 fields)

| Field | Type | Description |
|-------|------|-------------|
| `id` | `Guid` | Item identifier |
| `sellerId` | `Guid` | Owner/seller user ID |
| `categoryId` | `Guid?` | Category reference (nullable) |
| `title` | `string` | Item title |
| `description` | `string?` | Item description |
| `condition` | `string` | Item condition (e.g. `new`, `used`) |
| `status` | `string` | Item status |
| `quantity` | `int` | Available quantity |
| `images` | `IReadOnlyList<ItemMediaDto>` | List of media/images attached to the item |
| `createdAt` | `DateTime` | When the item was created |

### ItemMediaDto (12 fields)

| Field | Type | Description |
|-------|------|-------------|
| `id` | `Guid` | Media record ID |
| `url` | `string` | Secure URL |
| `publicId` | `string` | CDN public identifier |
| `resourceType` | `string` | Resource type (image, video, etc.) |
| `isPrimary` | `bool` | Whether this is the primary display image |
| `sortOrder` | `int` | Display order |
| `fileName` | `string?` | Original file name |
| `bytes` | `long?` | File size in bytes |
| `format` | `string?` | File format (e.g. `jpg`, `png`) |
| `width` | `int?` | Image width in pixels |
| `height` | `int?` | Image height in pixels |
| `durationSeconds` | `double?` | Duration for video/audio media |

---

## Source References

| File | Path |
|------|------|
| Endpoint | `src/presentation/OIO.Api/Endpoints/AuctionContext/Items/GetMyItemsEndpoint.cs` |
| Query | `src/core/OIO.Application/Context/AuctionContext/Queries/GetMyItems/GetMyItemsQuery.cs` |
| Filter | `src/core/OIO.Application/Context/AuctionContext/Queries/GetMyItems/GetMyItemsFilterParameters.cs` |
| DTO | `src/core/OIO.Application/Context/AuctionContext/DTOs/ItemDto.cs` |
| Media DTO | `src/core/OIO.Application/Context/AuctionContext/DTOs/ItemMediaDto.cs` |
| Mappings | `src/core/OIO.Application/Context/AuctionContext/Mappings/ItemMappings.cs` |
