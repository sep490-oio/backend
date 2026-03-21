# 06 - Auction Curation

Admin endpoint for managing auction visibility, priority ordering, featured status, and assigning an admin reviewer to an auction.

---

## Endpoint

| Method | Route | Permission |
|--------|-------|------------|
| `PUT` | `api/admin/auctions/{auctionId}/curation` | `Catalogs.Admin.ManageItems` |

---

## Request Body

```json
{
  "assignedAdminId": "guid | null",
  "clearAssignedAdmin": false,
  "priority": 10.0,
  "priorityReason": "High-value vintage item",
  "isFeatured": true
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `assignedAdminId` | `Guid?` | No | Admin user ID to assign as reviewer. Validated as non-empty GUID when provided. |
| `clearAssignedAdmin` | `bool` | No | When `true`, removes the currently assigned admin (sets `AssignedAdminId` to `null`). |
| `priority` | `decimal?` | No | Priority score for ordering. Must be non-negative when provided. |
| `priorityReason` | `string?` | No | JSON string explaining why this priority was set. Stored in `PriorityInfo` value object. |
| `isFeatured` | `bool?` | No | Mark or unmark the auction as featured. |

---

## Handler Logic

**Source:** `SetAuctionCurationCommandHandler`

1. **Load auction** by `AuctionId` with `Item` include.
2. **Resolve assigned admin:**
   - If `ClearAssignedAdmin == true` -> set to `null`.
   - If `AssignedAdminId` is provided -> use the new value.
   - Otherwise -> keep the existing `auction.AssignedAdminId`.
3. **Resolve priority:** if either `Priority` or `PriorityReason` is provided, create a new `PriorityInfo` value object using the provided values or falling back to the existing auction values. `PriorityInfo` has two fields: `Score` (decimal) and `Reason` (string, stored as JSONB).
4. **Call `auction.ApplyCuration(assignedAdminId, priority, isFeatured, nowUtc)`.**
5. **Save and return** the updated `AuctionDto`.

---

## Domain Validation (`Auction.ApplyCuration`)

The method rejects curation on terminal auction statuses:

| Rejected Status |
|----------------|
| `Failed` |
| `Sold` |
| `PaymentDefaulted` |
| `Cancelled` |
| `Terminated` |

Returns `AuctionErrors.Auction.InvalidState` with action `"curate"` when the auction is in one of these states.

When accepted:
- Updates `AssignedAdminId` and sets `AssignedAt` to `nowUtc` (or `null` if clearing).
- Updates `Priority` if a new `PriorityInfo` was provided.
- Updates `IsFeatured` if `isFeatured` has a value.
- Sets `ModifiedAt` to `nowUtc`.

---

## Response

`200 OK` with `AuctionDto` representing the updated auction state.

`422 Unprocessable Entity` for validation errors (empty GUID, negative priority, etc.).

---

## Key Source Files

| File | Path |
|------|------|
| Command + Handler | `src/core/OIO.Application/Context/AuctionContext/Commands/SetAuctionCuration/SetAuctionCurationCommand.cs` |
| Endpoint | `src/presentation/OIO.Api/Endpoints/AuctionContext/Admins/SetAuctionCurationEndpoint.cs` |
| Domain method | `src/core/OIO.Domain/Context/AuctionContext/Aggregates/Auctions/Auction.cs` (`ApplyCuration`) |
| Value object | `src/core/OIO.Domain/Context/AuctionContext/ValueObjects/PriorityInfo.cs` |
