# 01 -- Create & Read Items

## Overview

Sellers create items in `Draft` status with an optional inline images array. Items can then be retrieved by ID (public), listed by the owning seller, or browsed by seller profile.

---

## Endpoints

### 1. POST `/api/items` -- Create Item

**Auth:** `items.create`

Creates a new item in `Draft` status. Optionally attaches pre-uploaded media in the same request.

**Request body:**

```json
{
  "title": "Vintage Watch",
  "condition": "like_new",
  "categoryId": "3fa85f64-...",
  "description": "A rare 1960s timepiece",
  "quantity": 1,
  "attributes": { "brand": "Omega" },
  "images": [
    {
      "mediaUploadId": "3fa85f64-...",
      "isPrimary": true,
      "sortOrder": 0
    }
  ]
}
```

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `title` | `string` | Yes | Not whitespace, max 255 characters |
| `condition` | `string` | Yes | One of: `new`, `like_new`, `very_good`, `good`, `acceptable` |
| `categoryId` | `guid?` | No | Must be a valid, existing category ID |
| `description` | `string?` | No | Not whitespace (when provided) |
| `quantity` | `int` | No (default: 1) | Must be positive (> 0) |
| `attributes` | `object?` | No | Serialized as JSON |
| `images` | `MediaAttachment[]?` | No | Array of media references |

**MediaAttachment fields:**

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `mediaUploadId` | `guid` | Yes | Non-empty GUID |
| `isPrimary` | `bool` | Yes (default: false) | |
| `sortOrder` | `int` | No (default: 0) | |

**Media validation (when images provided):**

1. All referenced `MediaUpload` records must exist in the database.
2. Each upload must be owned by the current user (`upload.UserId == currentUser`).
3. Each upload must be confirmed (`upload.IsConfirmed == true`).
4. Each upload must not be already linked to another entity (`upload.IsLinked == false`).
5. Each upload must have an item-compatible context (`_contextRegistry.IsItemContext(upload.Context)` -- contexts `item_image` or `item_video`).
6. Per-type limits are enforced: `item_image` max 10, `item_video` max 3.

**Handler flow:**

1. Validate category exists (if provided).
2. Load and validate all referenced `MediaUpload` records.
3. Create `ItemTitle` value object (enforces max 255 chars).
4. Parse `ItemCondition` from string.
5. Call `Item.Create(...)` -- sets status to `Draft`, raises `ItemCreatedEvent`.
6. For each image: call `item.AddMedia(...)` which creates `ItemMedia` entities and links uploads.
7. Insert item, relocate media uploads to final storage, save.

**Response:** `201 Created`

```json
{
  "id": "3fa85f64-...",
  "sellerId": "3fa85f64-...",
  "categoryId": "3fa85f64-...",
  "title": "Vintage Watch",
  "description": "A rare 1960s timepiece",
  "condition": "like_new",
  "status": "draft",
  "quantity": 1,
  "images": [
    {
      "id": "3fa85f64-...",
      "url": "https://res.cloudinary.com/...",
      "publicId": "items/abc123",
      "resourceType": "image",
      "isPrimary": true,
      "sortOrder": 0,
      "fileName": "test-photo.jpg",
      "bytes": 204800,
      "format": "jpg",
      "width": 800,
      "height": 600,
      "durationSeconds": null
    }
  ],
  "createdAt": "2026-03-20T10:00:00Z"
}
```

---

### 2. GET `/api/items/{itemId}` -- Get Item by ID

**Auth:** Anonymous (no authentication required)

Returns a single item by its GUID.

**Path parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `itemId` | `guid` | Item ID |

**Response:** `200 OK` with `ItemDto`

---

### 3. GET `/api/items/my` -- Get My Items

**Auth:** Authenticated (any logged-in user)

Returns the current user's items as a paginated, sortable list.

**Query parameters:**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | `int` | 1 | Page number |
| `pageSize` | `int` | 20 | Items per page |
| `sortBy` | `string?` | - | Sort expression (e.g. `createdAt desc`) |

**Response:** `200 OK` with `PagedList<ItemDto>`

---

### 4. GET `/api/sellers/{sellerId}/items` -- Get Seller Items (Public)

**Auth:** Anonymous

Returns a public seller's items. Uses `GetPublicSellerItemsQuery` which filters to publicly visible items.

**Path parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `sellerId` | `guid` | Seller's user ID |

**Response:** `200 OK` with paginated item list

---

## ItemDto Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | `Guid` | Item ID |
| `sellerId` | `Guid` | Owner's user ID |
| `categoryId` | `Guid?` | Category reference |
| `title` | `string` | Item title |
| `description` | `string?` | Item description |
| `condition` | `string` | One of: `new`, `like_new`, `very_good`, `good`, `acceptable` |
| `status` | `string` | Current item status |
| `quantity` | `int` | Quantity available |
| `images` | `ItemMediaDto[]` | Attached media list |
| `createdAt` | `DateTime` | Creation timestamp |

---

## Error Codes

| Code | Type | Condition |
|------|------|-----------|
| `Category.NotFound` | 404 | Provided `categoryId` does not exist |
| `Media.NotFounds` | 404 | One or more `mediaUploadId` values not found |
| `Media.NotOwnedByUser` | 403 | Media upload belongs to a different user |
| `Media.NotConfirm` | 409 | Media upload has not been confirmed yet |
| `Media.AlreadyLinked` | 409 | Media upload is already linked to another entity |
| `Media.WrongContext` | 409 | Media upload context is not an item context |
| `Media.LimitReached` | 409 | Maximum media count for this resource type exceeded |
| `Item.NotFound` | 404 | Item with given ID not found |
| `Item.NotOwnedByUser` | 403 | Current user does not own this item |

---

## Source Files

- Domain entity: `src/core/OIO.Domain/Context/CatalogContext/Aggregates/Items/Item.cs`
- Command: `src/core/OIO.Application/Context/AuctionContext/Commands/CreateItem/CreateItemCommand.cs`
- Endpoint: `src/presentation/OIO.Api/Endpoints/AuctionContext/Items/CreateItemEndpoint.cs`
- DTO: `src/core/OIO.Application/Context/AuctionContext/DTOs/ItemDto.cs`
- Query (GetById): `src/core/OIO.Application/Context/AuctionContext/Queries/GetItemById/`
- Query (GetMyItems): `src/core/OIO.Application/Context/AuctionContext/Queries/GetMyItems/`
