# 08 - Enums Reference

Reference tables for the domain enums used throughout the item management flow.

---

## ItemStatus

Defined in `OIO.Domain.Context.CatalogContext.Enums.ItemStatus`.

10 values. `IsEditable` returns `true` only for `draft` and `rejected`.

| Value | Description |
|---|---|
| `draft` | Initial state after creation. Item can be edited, submitted, activated directly (if approved path not required), or removed. |
| `pending_verify` | Item submitted with `verifyByPlatform = true`. Awaiting physical inspection at the warehouse. |
| `pending_review` | Item submitted with `verifyByPlatform = false`. Awaiting admin content review. |
| `pending_condition_confirmation` | Warehouse inspection approved but the inspected condition differs from the seller's declaration. Seller must confirm the new condition. |
| `approved` | Item passed review or inspection. Ready to be activated or used for auction creation. |
| `rejected` | Item was rejected by admin review or platform inspection. Can be edited and resubmitted. |
| `active` | Item is activated and available for auction creation. |
| `in_auction` | Item is currently linked to a live auction. |
| `sold` | Item has been sold through an auction. |
| `removed` | Item has been removed from the platform. Terminal state from most statuses. |

### Status Transition Table

| From | To | Trigger |
|---|---|---|
| `draft` | `pending_verify` | `item.Submit(verifyByPlatform: true)` |
| `draft` | `pending_review` | `item.Submit(verifyByPlatform: false)` |
| `draft` | `active` | `item.Activate()` |
| `draft` | `removed` | `item.Remove()` |
| `pending_verify` | `approved` | `item.ApproveFromPlatformInspection()` — inspection approved, condition matches |
| `pending_verify` | `rejected` | `item.RejectFromPlatformInspection()` — inspection rejected |
| `pending_verify` | `pending_condition_confirmation` | `item.RequireConditionConfirmation()` — condition mismatch |
| `pending_review` | `approved` | `item.Approve(adminId)` — admin approves |
| `pending_review` | `rejected` | `item.Reject(adminId, reason)` — admin rejects |
| `pending_condition_confirmation` | `approved` | `item.ConfirmInspectedCondition()` — seller confirms |
| `pending_condition_confirmation` | `rejected` | Structurally allowed by `CanTransitionTo` |
| `rejected` | `pending_verify` | `item.Resubmit(verifyByPlatform: true)` |
| `rejected` | `pending_review` | `item.Resubmit(verifyByPlatform: false)` |
| `rejected` | `removed` | `item.Remove()` |
| `approved` | `in_auction` | `item.MarkInAuction()` — auction goes live |
| `approved` | `removed` | `item.Remove()` |
| `active` | `in_auction` | `item.MarkInAuction()` — auction goes live |
| `active` | `removed` | `item.Remove()` |
| `in_auction` | `sold` | `item.MarkSold()` — auction completed with winner |
| `in_auction` | `active` | `item.ReturnToActive()` — auction cancelled or ended without sale |
| `sold` | `removed` | `item.Remove()` |

---

## ItemCondition

Defined in `OIO.Domain.Context.CatalogContext.Enums.ItemCondition`.

5 values representing the seller-declared physical condition of the item.

| Value | Description |
|---|---|
| `new` | Brand new, unused, in original packaging. |
| `like_new` | Opened but barely used, virtually indistinguishable from new. |
| `very_good` | Minor signs of use but fully functional, no significant cosmetic flaws. |
| `good` | Noticeable wear or cosmetic imperfections, fully functional. |
| `acceptable` | Significant wear or cosmetic damage, still functional. |

> **Note:** The warehouse inspection uses `WarehouseItemCondition` which includes an additional `damaged` value not present in `ItemCondition`. When the inspector records `damaged`, the review decision maps accordingly (typically rejection or condition confirmation).

---

## ModerationAction

Defined in `OIO.Domain.Context.CatalogContext.Enums.ModerationAction`.

11 values recorded in `ItemModerationReview` entries to track every moderation action on an item.

| Value | Description |
|---|---|
| `submitted` | Seller submitted the item for review. Records the transition from `draft` to `pending_verify` or `pending_review`. |
| `assigned` | An admin was assigned as reviewer. Status does not change (old and new status are the same). |
| `started_review` | Admin started reviewing the item. |
| `approved` | Admin approved the item via content review. Transitions to `approved`. |
| `rejected` | Admin rejected the item via content review. Transitions to `rejected`. Includes a reason. |
| `platform_verified` | Platform warehouse inspection approved the item (condition matches). Transitions `pending_verify` to `approved`. |
| `platform_rejected` | Platform warehouse inspection rejected the item. Transitions `pending_verify` to `rejected`. Includes a reason. |
| `condition_confirmation_requested` | Inspection approved but condition differs. Transitions `pending_verify` to `pending_condition_confirmation`. |
| `condition_confirmed` | Seller confirmed the inspected condition. Transitions `pending_condition_confirmation` to `approved`. |
| `resubmitted` | Seller resubmitted a rejected item. Increments `ResubmissionCount`. Transitions `rejected` to `pending_verify` or `pending_review`. |
| `removed` | Item was removed from the platform. |

### ModerationAction by Status Transition

| Action | Old Status | New Status | Actor |
|---|---|---|---|
| `submitted` | `draft` | `pending_verify` or `pending_review` | Seller |
| `assigned` | _(unchanged)_ | _(unchanged)_ | Admin |
| `started_review` | — | — | Admin |
| `approved` | `pending_review` | `approved` | Admin |
| `rejected` | `pending_review` | `rejected` | Admin |
| `platform_verified` | `pending_verify` | `approved` | Inspector/Reviewer |
| `platform_rejected` | `pending_verify` | `rejected` | Inspector/Reviewer |
| `condition_confirmation_requested` | `pending_verify` | `pending_condition_confirmation` | Reviewer |
| `condition_confirmed` | `pending_condition_confirmation` | `approved` | Seller |
| `resubmitted` | `rejected` | `pending_verify` or `pending_review` | Seller |
| `removed` | _(various)_ | `removed` | Seller/Admin |
