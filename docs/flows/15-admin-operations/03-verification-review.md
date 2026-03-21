# 03 - Verification Review

## Overview

Admins review identity verifications submitted by users. The verification review flow handles listing pending/submitted verifications, viewing full details, and approving or rejecting them. Approval includes a duplicate identity check to prevent the same identity document from being used across multiple accounts.

---

## Verification Status Transitions (Admin Actions)

**IdentityVerificationStatus values:** `pending`, `submitted`, `under_review`, `approved`, `rejected`, `expired`, `suspended`

The admin review handles the following transitions:

| From Status | Action | To Status |
|---|---|---|
| `submitted` | Admin views in queue | (visible in pending list) |
| `under_review` | Admin views in queue | (visible in pending list) |
| `submitted` / `under_review` | Approve | `approved` |
| `submitted` / `under_review` | Reject | `rejected` |

**Note:** Only verifications in `submitted` or `under_review` status appear in the pending queue. The `Approve` and `Reject` domain methods validate the current status and return errors for invalid transitions (`CannotApproveInCurrentStatus`, `CannotRejectInCurrentStatus`).

---

## Endpoints

### 1. GET `api/admin/verifications` -- List Pending Verifications

**Permission:** `admin:verifications:read`

**Handler (`GetPendingVerificationsQueryHandler`):**
- Queries `IdentityVerification` entities where `Status == Submitted` or `Status == UnderReview`
- Orders by `SubmittedAt` ascending (oldest first)
- Returns as `IReadOnlyCollection<VerificationSummaryDto>`

**Response (`VerificationSummaryDto`):**

| Field | Type |
|---|---|
| `Id` | Guid |
| `VerificationType` | string |
| `AutoVerified` | bool |
| `FullName` | string? |
| `Status` | string |
| `SubmittedAt` | DateTime? |
| `AttemptCount` | int |
| `CreatedAt` | DateTime |

---

### 2. GET `api/admin/verifications/{verificationId}` -- Get Verification by ID

**Permission:** `admin:verifications:read`

**Path parameter:** `verificationId` (Guid, non-empty)

**Response (`VerificationDto`):**

| Field | Type |
|---|---|
| `Id` | Guid |
| `UserId` | Guid |
| `VerificationType` | string |
| `AutoVerified` | bool |
| `FullName` | string? |
| `DateOfBirth` | DateOnly? |
| `Gender` | string? |
| `Nationality` | string? |
| `Document` | `VerificationDocumentInfoDto?` (IdType, IdNumber, IssuedDate, ExpiredDate, IssuedPlace) |
| `PermanentAddress` | `VerificationAddressDto?` (FullAddress, Province, District, Ward) |
| `Status` | string |
| `VerifiedAt` | DateTime? |
| `VerifiedBy` | Guid? |
| `RejectionReason` | string? |
| `RejectionCode` | string? |
| `SubmittedAt` | DateTime? |
| `ExpiresAt` | DateTime? |
| `AttemptCount` | int |
| `CreatedAt` | DateTime |
| `ModifiedAt` | DateTime? |
| `Documents` | `VerificationDocumentDto[]` |

---

### 3. POST `api/admin/verifications/{verificationId}/approve` -- Approve Verification

**Permission:** `admin:verifications:manage`

**Request (`ApproveVerificationCommand`):** `VerificationId` (Guid)

**Handler logic (`ApproveVerificationCommandHandler`):**
1. Loads verification with documents
2. Checks for duplicate identity via `VerificationDuplicateIdentityService.FindDuplicateAsync()` -- returns `DuplicateIdentity` error if the same identity document is already associated with another account
3. Calls `verification.Approve(currentUserId, nowUtc)`
4. Persists changes

**Side effects:** On approval, the verification's `VerifiedAt` and `VerifiedBy` fields are set. The user can then proceed to create a seller profile (which requires an approved verification).

---

### 4. POST `api/admin/verifications/{verificationId}/reject` -- Reject Verification

**Permission:** `admin:verifications:manage`

**Request (`RejectVerificationCommand`):**

| Field | Type | Required | Notes |
|---|---|---|---|
| `VerificationId` | Guid | Yes | From route |
| `Reason` | string | Yes | Max 1000 characters |
| `RejectionCode` | string | No | Max 50 characters, optional code for categorization |

**Handler logic (`RejectVerificationCommandHandler`):**
1. Loads verification with documents
2. Calls `verification.Reject(currentUserId, reason, nowUtc, rejectionCode)`
3. Persists changes

**After rejection:** The user can update their verification and resubmit (status returns to `pending`, then `submitted` on resubmission).

---

## Error Codes

| Code | HTTP | Description |
|---|---|---|
| `Verification.NotFound` | 404 | Verification with given ID not found |
| `Verification.CannotApprove` | 403 | Cannot approve in current status |
| `Verification.CannotReject` | 403 | Cannot reject in current status |
| `Verification.DuplicateIdentity` | 409 | Identity document already associated with another account |

---

## Source References

- `src/core/OIO.Application/Context/UserContext/Queries/GetPendingVerifications/`
- `src/core/OIO.Application/Context/UserContext/Queries/GetVerificationById/`
- `src/core/OIO.Application/Context/UserContext/Commands/ApproveVerification/`
- `src/core/OIO.Application/Context/UserContext/Commands/RejectVerification/`
- `src/core/OIO.Application/Context/UserContext/Services/VerificationDuplicateIdentityService.cs`
- `src/core/OIO.Domain/Context/UserContext/Enums/IdentityVerificationStatus.cs`
