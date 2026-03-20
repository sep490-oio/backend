# 05 -- Upload Contexts Reference

## Overview

The platform defines 8 upload contexts in `appsettings.Production.json` under `Media.UploadContexts`. Each context specifies the allowed resource type, Cloudinary folder, file size limit, permitted formats, eager transformations, and maximum uploads per entity.

---

## Contexts Table

| Name | ResourceType | Folder | MaxSize | Formats | Eager | MaxPerEntity |
|------|-------------|--------|---------|---------|-------|-------------|
| `item_image` | `image` | `items` | 10 MB (10,485,760 bytes) | jpg, jpeg, png, webp | `w_800,h_800,c_limit\|w_400,h_400,c_fill` | 10 |
| `item_video` | `video` | `items` | 100 MB (104,857,600 bytes) | mp4, mov, webm | `w_720,c_limit,q_auto/mp4` | 3 |
| `user_avatar` | `image` | `avatars` | 5 MB (5,242,880 bytes) | jpg, jpeg, png, webp | `w_200,h_200,c_fill` | 1 |
| `verification_document` | `image` | `verifications` | 10 MB (10,485,760 bytes) | jpg, jpeg, png, webp | `w_1200,h_1200,c_limit` | 10 |
| `verification_image` | `image` | `verifications` | 10 MB (10,485,760 bytes) | jpg, jpeg, png, webp | `w_1600,h_1600,c_limit` | 10 |
| `warehouse_inspection_image` | `image` | `warehouse/inspections` | 10 MB (10,485,760 bytes) | jpg, jpeg, png, webp | `w_1600,h_1600,c_limit` | 10 |
| `dispute_attachment` | `image` | `disputes` | 10 MB (10,485,760 bytes) | jpg, jpeg, png, webp | `w_1600,h_1600,c_limit` | 10 |
| `term_document` | `document` | `terms` | 10 MB (10,485,760 bytes) | pdf | _(none)_ | 1 |

> **Note on `document` resource type**: The `term_document` context uses `"document"` as its `ResourceType` in config, which `UploadContextRegistry.ParseResourceType()` maps to `MediaResourceType.Raw`. The Cloudinary upload URL will use the `raw` path segment: `https://api.cloudinary.com/v1_1/{cloudName}/raw/upload`.

---

## GET /api/media/contexts Endpoint

| | |
|---|---|
| **Method** | `GET` |
| **Path** | `/api/media/contexts` |
| **Permission** | `media:contexts:read` |
| **Handler** | `GetUploadContextsQueryHandler` |

Returns the list of all upload contexts as `UploadContextDto[]`:

```json
[
  {
    "name": "item_image",
    "resourceType": "image",
    "maxFileSizeBytes": 10485760,
    "allowedFormats": ["jpg", "jpeg", "png", "webp"],
    "maxUploadsPerEntity": 10
  },
  {
    "name": "item_video",
    "resourceType": "video",
    "maxFileSizeBytes": 104857600,
    "allowedFormats": ["mp4", "mov", "webm"],
    "maxUploadsPerEntity": 3
  }
]
```

The DTO exposes a subset of the configuration. Fields **not** exposed to the client:
- `Folder` (internal storage structure)
- `Eager` (Cloudinary transformation detail)

---

## UploadContextRegistry

The `UploadContextRegistry` class (registered as a singleton) provides helper methods for working with contexts:

### Lookup Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| `Get` | `UploadContextOption? Get(string contextName)` | Returns the config for a context name (case-insensitive), or `null` |
| `GetAllContext` | `string[] GetAllContext()` | Returns all registered context names |
| `IsValid` | `bool IsValid(string contextName)` | Checks if a context name exists |
| `ValidateContext` | `UnitResult<Error> ValidateContext(string contextName)` | Returns error if empty or not in the registered set |

### Context Classification Methods

The registry classifies contexts by their **name prefix** to determine which entity type they belong to:

| Method | Prefix Match | Used By |
|--------|-------------|---------|
| `IsItemContext` | `item_` | Items (images, videos) |
| `IsUserContext` | `user_` | User-related media |
| `IsUserAvatarContext` | exact `user_avatar` | User avatar specifically |
| `IsTermContext` | `term_` | Terms documents |
| `IsCategoryContext` | `category_` | Category icons |
| `IsDisputeContext` | `dispute_` | Dispute message attachments |
| `IsVerificationContext` | `verification_` | Identity verification documents |
| `IsWarehouseInspectionContext` | `warehouse_inspection_` | Warehouse inspection evidence |

### Resource Limits

| Method | Signature | Description |
|--------|-----------|-------------|
| `GetMaxUploadsForContext` | `int GetMaxUploadsForContext(string contextName)` | Returns `MaxUploadsPerEntity` for a context (default 10) |
| `GetMaxForEntityMedia` | `int GetMaxForEntityMedia(string entityPrefix, string resourceType)` | Finds the first context matching prefix and resource type, returns its limit |

### Resource Type Parsing

`UploadContextRegistry.ParseResourceType(string)` maps config strings to the `MediaResourceType` enum:

| Config Value | Enum Value | Cloudinary Path | File Prefix |
|-------------|------------|-----------------|-------------|
| `"image"` | `MediaResourceType.Image` | `image` | `img` |
| `"video"` | `MediaResourceType.Video` | `video` | `vid` |
| `"raw"` | `MediaResourceType.Raw` | `raw` | `file` |
| `"document"` | `MediaResourceType.Raw` | `raw` | `file` |

---

## Context Naming Convention

Contexts follow a `{entityPrefix}_{mediaType}` naming pattern:

| Prefix | Classification | Entity |
|--------|---------------|--------|
| `item_` | Item context | `Item` aggregate |
| `user_` | User context | `User` aggregate |
| `verification_` | Verification context | `IdentityVerification` aggregate |
| `warehouse_inspection_` | Warehouse inspection context | `WarehouseInspection` aggregate |
| `dispute_` | Dispute context | `DisputeMessageAttachment` entity |
| `term_` | Term context | `TermsDocument` entity |
| `category_` | Category context | `Category` aggregate |

This prefix convention is used by the `MediaRelocationService` to determine which entity to load and update when relocating media to its final folder.

---

## UploadContextOption Properties

The `UploadContextOption` class defines the configuration shape:

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Context name (e.g. `item_image`) |
| `ResourceType` | `string` | Cloudinary resource type: `image`, `video`, `document` |
| `Folder` | `string` | Base Cloudinary folder (before `/pending/{userId}` is appended) |
| `MaxFileSizeBytes` | `long` | Maximum file size in bytes |
| `AllowedFormats` | `string[]` | Permitted file extensions |
| `Eager` | `string?` | Cloudinary eager transformation string (null for no transforms) |
| `MaxUploadsPerEntity` | `int` | Maximum number of uploads allowed per linked entity |
