# 03 - Dispute Overview

## Dispute Entity

`Dispute` is an `AggregateRoot<DisputeId>` implementing `IAuditableEntity`.

### Core Properties

| Property | Type | Description |
|----------|------|-------------|
| DisputeNumber | DisputeNumber | Format: `DSP-{guid:N}` truncated to 16 chars |
| OrderId | OrderId | Linked order (Guid.Empty if no order) |
| AuctionId | AuctionId? | Linked auction (null for verification disputes) |
| VerificationId | IdentityVerificationId? | Linked verification (null for auction disputes) |
| ComplainantId | UserId | User who filed the dispute |
| RespondentId | UserId | User the dispute is against |
| Type | DisputeType | Category of dispute |
| Title | string | Dispute title |
| Description | string | Dispute description |
| DesiredResolution | DesiredResolution | What the complainant wants |
| Status | DisputeStatus | Current lifecycle status |
| Priority | DisputePriority | Urgency level |
| ResolutionType | ResolutionType | How the dispute was resolved (default: NoResolution) |
| ResolutionNotes | string? | Admin notes on resolution |
| ResolutionAmount | decimal? | Monetary resolution amount |
| AssignedTo | UserId? | Admin handling the dispute |
| EscalatedTo | UserId? | Senior admin if escalated |
| ResponseDeadline | DateTime? | Deadline for response |
| EscalatedAt | DateTime? | When escalated |
| ResolvedAt | DateTime? | When resolved |
| ClosedAt | DateTime? | When closed |
| CreatedAt | DateTime | Creation timestamp |
| ModifiedAt | DateTime? | Last modification |

### Collections

| Collection | Type |
|-----------|------|
| Evidence | `IReadOnlyCollection<DisputeEvidence>` |
| Messages | `IReadOnlyCollection<DisputeMessage>` |
| Refunds | `IReadOnlyCollection<DisputeRefund>` |
| StatusHistory | `IReadOnlyCollection<DisputeStatusHistory>` |

## Creation Paths

### Create() -- Auction Disputes

```csharp
Dispute.Create(
    auctionId, complainantId, respondentId, type, title, description, nowUtc,
    desiredResolution?, priority?, orderId?)
```

- Requires `auctionId` (non-null)
- Sets `Status = Open`, `Priority = Medium` (default), `DesiredResolution = NoAction` (default), `ResolutionType = NoResolution`
- Adds initial `DisputeStatusHistory` entry: `null -> open`, reason "Dispute created"

### CreateForVerification() -- Verification Disputes

```csharp
Dispute.CreateForVerification(
    verificationId, complainantId, respondentId, type, title, description, nowUtc,
    desiredResolution?, priority?)
```

- Requires `verificationId` (non-null)
- Same defaults as Create()
- `OrderId` defaults to `Guid.Empty`

### Validation

Both paths call `CreateCore()` which validates:
- At least one of `auctionId` or `verificationId` must be non-null -- error `Dispute.ReferenceRequired`

## Child Entities

### DisputeMessage

| Field | Type | Description |
|-------|------|-------------|
| Id | DisputeMessageId | Message ID |
| DisputeId | DisputeId | Parent dispute |
| SenderId | UserId | Message sender |
| Message | string | Message text |
| IsInternal | bool | Admin-only message |
| CreatedAt | DateTime | Sent at |
| Attachments | IReadOnlyCollection\<DisputeMessageAttachment\> | Attached media |

### DisputeMessageAttachment

| Field | Type | Description |
|-------|------|-------------|
| Id | DisputeMessageAttachmentId | Attachment ID |
| DisputeId | DisputeId | Parent dispute |
| DisputeMessageId | DisputeMessageId | Parent message |
| MediaUploadId | MediaUploadId | Linked media upload |
| SortOrder | int | Display order |
| StorageRef | StorageRef | Cloud storage reference |
| Info | MediaInfo | File metadata (FileName, SecureUrl, Bytes, Format, Width, Height, DurationSeconds) |
| CreatedAt | DateTime | Created at |
| ModifiedAt | DateTime? | Updated at |

### DisputeEvidence

| Field | Type | Description |
|-------|------|-------------|
| Id | DisputeEvidenceId | Evidence ID |
| DisputeId | DisputeId | Parent dispute |
| SubmittedBy | UserId | Who submitted |
| Type | EvidenceType | Evidence category |
| EvidenceStorage | StorageRef? | Cloud storage |
| EvidenceInfo | MediaInfo? | File metadata |
| Description | string? | Description |
| CreatedAt | DateTime | Created at |

### DisputeRefund

| Field | Type | Description |
|-------|------|-------------|
| Id | DisputeRefundId | Refund ID |
| DisputeId | DisputeId | Parent dispute |
| TransactionId | TransactionId | Linked payment transaction (UNIQUE) |
| RefundType | RefundType | Refund category |
| Reason | string | Reason for refund |
| ApprovedBy | UserId? | Admin who approved |
| ApprovedAt | DateTime? | When approved |
| Notes | string? | Additional notes |
| CreatedAt | DateTime | Created at |

### DisputeStatusHistory

| Field | Type | Description |
|-------|------|-------------|
| Id | DisputeStatusHistoryId | History entry ID |
| DisputeId | DisputeId | Parent dispute |
| OldStatus | string? | Previous status (null for initial) |
| NewStatus | string | New status |
| ChangedBy | UserId? | Who triggered the change |
| Reason | string? | Reason for change |
| CreatedAt | DateTime | When changed |

### DisputeParticipantState

| Field | Type | Description |
|-------|------|-------------|
| Id | DisputeParticipantStateId | State ID |
| DisputeId | DisputeId | Parent dispute |
| UserId | UserId | Participant |
| LastReadMessageId | DisputeMessageId? | Last message they read |
| LastReadAt | DateTime? | When they last read |
| LastSeenAt | DateTime? | When they last opened the thread |
| CreatedAt | DateTime | State created at |
| ModifiedAt | DateTime? | Last updated |

Methods:
- `MarkRead(lastReadMessageId, readAt)` -- updates LastReadMessageId, LastReadAt, LastSeenAt, ModifiedAt
- `Touch(nowUtc)` -- updates LastSeenAt and ModifiedAt only

## Enums

### DisputeType (9 values)

| Value | String ID |
|-------|-----------|
| ItemNotReceived | `item_not_received` |
| ItemNotAsDescribed | `item_not_as_described` |
| DamagedItem | `damaged_item` |
| Counterfeit | `counterfeit` |
| PaymentIssue | `payment_issue` |
| ShippingIssue | `shipping_issue` |
| SellerUnresponsive | `seller_unresponsive` |
| VerificationCorrection | `verification_correction` |
| Other | `other` |

### DesiredResolution (5 values)

| Value | String ID |
|-------|-----------|
| NoAction | `no_action` |
| Refund | `refund` |
| Replacement | `replacement` |
| PartialRefund | `partial_refund` |
| Other | `other` |

### DisputeStatus (8 values)

| Value | String ID |
|-------|-----------|
| Draft | `draft` |
| Open | `open` |
| UnderReview | `under_review` |
| AwaitingResponse | `awaiting_response` |
| Escalated | `escalated` |
| Resolved | `resolved` |
| Closed | `closed` |
| Cancelled | `cancelled` |

### DisputePriority (5 values)

| Value | String ID |
|-------|-----------|
| None | `none` |
| Low | `low` |
| Medium | `medium` |
| High | `high` |
| Urgent | `urgent` |

### ResolutionType (9 values)

| Value | String ID |
|-------|-----------|
| NoResolution | `no_resolution` |
| RefundFull | `refund_full` |
| RefundPartial | `refund_partial` |
| Replacement | `replacement` |
| FavorBuyer | `favor_buyer` |
| FavorSeller | `favor_seller` |
| MutualAgreement | `mutual_agreement` |
| NoAction | `no_action` |
| Cancelled | `cancelled` |

### EvidenceType (7 values)

| Value | String ID |
|-------|-----------|
| Image | `image` |
| Video | `video` |
| Document | `document` |
| Screenshot | `screenshot` |
| Receipt | `receipt` |
| Tracking | `tracking` |
| Other | `other` |

### RefundType (4 values)

| Value | String ID |
|-------|-----------|
| Full | `full` |
| Partial | `partial` |
| ShippingOnly | `shipping_only` |
| Compensation | `compensation` |

## DisputeAccessService

Controls who can access dispute data. Injected into all dispute handlers.

| Property / Method | Logic |
|-------------------|-------|
| `IsAdmin` | `currentUser.IsInRole(App.Roles.Catalogs.Admin)` |
| `CanViewInternalMessages` | Same as `IsAdmin` |
| `CurrentUserId` | `currentUser.UserId` |
| `GetAccessibleDisputeAsync(disputeId)` | Load dispute; return 404 if missing; return 403 if not admin, complainant, or respondent |
| `EnsureInternalMessageAllowed(isInternal)` | If `isInternal && !IsAdmin` -> 403 `Dispute.InternalMessageForbidden` |
| `CanAccess(dispute)` | `IsAdmin \|\| dispute.ComplainantId == userId \|\| dispute.RespondentId == userId` |
| `EnsureParticipantStateAsync(disputeId)` | Load or create `DisputeParticipantState` for current user; calls `Touch(nowUtc)` if existing |

## DisputeResponseTemplate

Pre-defined response templates for admin use.

| Field | Type | Description |
|-------|------|-------------|
| Id | DisputeResponseTemplateId | Template ID |
| Name | string | Template name |
| Category | string? | Category grouping |
| Subject | string? | Subject line |
| Body | string | Template body text |
| IsActive | bool | Whether available for use |
| CreatedAt | DateTime | Created at |
