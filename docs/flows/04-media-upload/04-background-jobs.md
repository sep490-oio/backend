# 04 -- Background Jobs

## Overview

Two Quartz-scheduled background jobs manage the media lifecycle after upload and confirmation:

1. **PendingUploadRelocationJob** -- Moves linked uploads from their temporary `pending/` folder to the final Cloudinary path and refreshes the entity snapshot.
2. **PendingUploadCleanupJob** -- Removes expired, orphaned, and old linked upload records (and their Cloudinary resources where applicable).

Both jobs run every **15 minutes**, are marked `[DisallowConcurrentExecution]`, and use `WithMisfireHandlingInstructionFireNow()`.

---

## Relocation Job

### Flow

```mermaid
---
config:
  layout: elk
---
flowchart TD
    Start([PendingUploadRelocationJob.Execute]) --> Query[Query candidates:<br/>IsLinked = true<br/>RelocatedAt = null<br/>Folder contains '/pending/'<br/>NextRelocationAttemptAt is null or <= now<br/>Order by earliest pending<br/>Take 50]
    Query --> Check{Candidates > 0?}
    Check -->|No| Done([Return])
    Check -->|Yes| Loop[For each upload]
    Loop --> RequiresReloc{RequiresRelocation?}
    RequiresReloc -->|No| Skip[Skip]
    RequiresReloc -->|Yes| CheckRetryTime{NextRelocationAttemptAt<br/>still in future?}
    CheckRetryTime -->|Yes| Skip
    CheckRetryTime -->|No| CheckEntity{EntityId present?}
    CheckEntity -->|No| Retry1[ScheduleRetry:<br/>'Missing entity id']
    CheckEntity -->|Yes| GetConfig[Get UploadContextOption<br/>from registry]
    GetConfig --> ExtractLeaf[Extract leaf from PublicId]
    ExtractLeaf --> ResolveFolder[ResolveTargetFolder:<br/>context.Folder / entityId]
    ResolveFolder --> SamePath{Old path ==<br/>Target path?}
    SamePath -->|Yes| MarkDone1[MarkRelocationSucceeded<br/>with existing storage]
    SamePath -->|No| Rename[Cloudinary RenameAsync:<br/>oldPublicId -> targetPublicId]
    Rename --> RenameOk{Rename succeeded?}
    RenameOk -->|No| Retry2[ScheduleRetry:<br/>'Rename failed']
    RenameOk -->|Yes| UpdateRef[Create new StorageRef<br/>from rename result]
    UpdateRef --> RefreshUrl[ResolveSecureUrl:<br/>prefer renamed URL,<br/>fallback: string replace]
    RefreshUrl --> RefreshEntity[RefreshLinkedEntitySnapshot:<br/>update entity media references]
    RefreshEntity --> RefreshOk{Refresh succeeded?}
    RefreshOk -->|No| Retry3[ScheduleRetry]
    RefreshOk -->|Yes| MarkDone2[MarkRelocationSucceeded:<br/>update StorageRef + MediaInfo<br/>RelocatedAt = now]
    MarkDone1 --> Next[Next upload]
    MarkDone2 --> Next
    Retry1 --> Next
    Retry2 --> Next
    Retry3 --> Next
    Skip --> Next
    Next --> Loop
    Loop --> Save[SaveChangesAsync]
    Save --> Done
```

### Retry Backoff

When relocation fails, `ScheduleRetry` sets `NextRelocationAttemptAt` based on the current `RelocationAttemptCount`:

| Attempt | Delay | NextRelocationAttemptAt |
|---------|-------|------------------------|
| 0 | 1 minute | `now + 1 min` |
| 1 | 5 minutes | `now + 5 min` |
| 2 | 15 minutes | `now + 15 min` |
| >= 3 | Permanent failure | `null` (no more retries) |

After 3 failed attempts (`MaxRetries = 3`), `NextRelocationAttemptAt` is set to `null` and the upload will no longer be picked up. The `LastRelocationError` field stores the reason.

### Job Configuration

| Setting | Value |
|---------|-------|
| **Job key** | `pending-upload-relocation` (group: `system`) |
| **Trigger key** | `pending-upload-relocation-trigger` |
| **Interval** | Every 15 minutes |
| **Batch size** | 50 per execution |
| **Concurrency** | `[DisallowConcurrentExecution]` |
| **Misfire** | `WithMisfireHandlingInstructionFireNow()` |

### Target Folder Resolution

The target folder depends on the upload context:

| Context Prefix | Target Folder Pattern | Example |
|----------------|-----------------------|---------|
| `item_*` | `items/{entityId}` | `items/019abc...` |
| `user_avatar` | `avatars/{entityId}` | `avatars/019def...` |
| `verification_*` | `verifications/{entityId}` | `verifications/019ghi...` |
| `term_*` | `terms/{entityId}` | `terms/019jkl...` |
| `category_*` | `categories/{entityId}` | `categories/019mno...` |
| `warehouse_inspection_*` | `warehouse/inspections/{entityId}` | `warehouse/inspections/019pqr...` |
| `dispute_*` | `disputes/{disputeId}/messages/{messageId}` | `disputes/019abc.../messages/019def...` |

General formula: `{contextConfig.Folder}/{entityId}`, except for `dispute_attachment` which resolves through the `DisputeMessageAttachment` entity to build a nested path.

### Entity Snapshot Refresh

After renaming on Cloudinary, the service updates the media reference on the linked entity:

| Context | Entity | Method Called |
|---------|--------|-------------|
| `item_*` | `Item` (with `Media` included) | `RefreshMediaSnapshot(oldPublicId, storageRef, info, now)` |
| `verification_*` | `IdentityVerification` (with `Documents`) | `RefreshDocumentSnapshot(...)` |
| `user_avatar` | `User` (with `Profile`) | `RefreshAvatarSnapshot(oldPublicId, avatarUrl, now)` |
| `term_*` | `TermsDocument` | `RefreshMediaSnapshot(storageRef, info)` |
| `category_*` | `Category` | `RefreshIconMedia(storageRef, info, now)` |
| `warehouse_inspection_*` | `WarehouseInspection` | `RefreshEvidenceSnapshot(...)` |
| `dispute_*` | `DisputeMessageAttachment` | `RefreshMediaSnapshot(storageRef, info, now)` |

---

## Cleanup Job

### Flow

```mermaid
---
config:
  layout: elk
---
flowchart TD
    Start([PendingUploadCleanupJob.Execute]) --> ComputeThresholds[Compute thresholds:<br/>orphanThreshold = now - OrphanExpiration<br/>linkedRetentionThreshold = now - LinkedRecordRetention]

    ComputeThresholds --> Case1[Case 1: Query expired unconfirmed<br/>WHERE IsConfirmed = false<br/>AND ExpiresAt < now]
    Case1 --> Case1Add[Add to toDelete list]

    Case1Add --> Case2[Case 2: Query orphan confirmed<br/>WHERE IsConfirmed = true<br/>AND IsLinked = false<br/>AND ConfirmedAt < orphanThreshold]
    Case2 --> Case2Add[Add to toDelete list]

    Case2Add --> Case3[Case 3: Query old linked records<br/>WHERE IsLinked = true<br/>AND LinkedAt < linkedRetentionThreshold<br/>AND Folder NOT contains '/pending/']
    Case3 --> Case3Remove[DB RemoveRange only<br/>keep Cloudinary resources]

    Case3Remove --> HasDeletes{toDelete.Count > 0?}
    HasDeletes -->|No| CheckSave{Any changes?}
    HasDeletes -->|Yes| FilterConfirmed[Filter confirmed uploads<br/>from toDelete list]
    FilterConfirmed --> BatchDelete[Batch delete from Cloudinary<br/>grouped by ResourceType]
    BatchDelete --> DbRemove[DB RemoveRange toDelete]
    DbRemove --> CheckSave

    CheckSave -->|No changes| Done([Return])
    CheckSave -->|Has changes| Save[SaveChangesAsync]
    Save --> Done
```

### Three Cleanup Cases

| Case | Condition | Cloudinary Action | DB Action |
|------|-----------|-------------------|-----------|
| **1. Expired unconfirmed** | `IsConfirmed = false` AND `ExpiresAt < now` | Delete (only if confirmed, so typically none) | Remove record |
| **2. Orphan confirmed** | `IsConfirmed = true` AND `IsLinked = false` AND `ConfirmedAt < orphanThreshold` (60 min) | Batch delete by resource type | Remove record |
| **3. Old linked** | `IsLinked = true` AND `LinkedAt < linkedRetentionThreshold` (7 days) AND folder does NOT contain `/pending/` | **No deletion** (resource is in its final folder and in use) | Remove record (audit trail cleanup) |

### Cloudinary Batch Deletion

For Cases 1 and 2, confirmed uploads are grouped by `ResourceType` (`image`, `video`, `raw`) and batch-deleted using `CloudinarySignatureService.DeleteResourcesAsync()`. Unconfirmed uploads in Case 1 do not have Cloudinary resources to delete (the upload may have never completed).

### Job Configuration

| Setting | Value |
|---------|-------|
| **Job key** | `pending-upload-cleanup` (group: `system`) |
| **Trigger key** | `pending-upload-cleanup-trigger` |
| **Interval** | Every 15 minutes |
| **Concurrency** | `[DisallowConcurrentExecution]` |
| **Misfire** | `WithMisfireHandlingInstructionFireNow()` |
