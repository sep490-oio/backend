# Seller Verification & Onboarding

This module covers the full journey from identity verification to becoming an active seller on the OIO Auction Platform. A user must complete identity verification (eKYC) before they can create a seller profile, which then goes through its own approval workflow.

**Bounded contexts involved:** `UserContext` (identity verification, seller profiles), `MediaContext` (document uploads), `NotificationContext` (outcome alerts), `ModerationContext` (correction disputes).

---

## Verification Status Lifecycle

The `IdentityVerification` aggregate tracks status through seven states defined in `IdentityVerificationStatus`.

```mermaid
---
config:
  layout: elk
---
stateDiagram-v2
    [*] --> Pending : Create verification
    Pending --> Submitted : Submit (documents attached)
    Submitted --> Approved : Auto-approve (score >= 80)
    Submitted --> Rejected : Auto-reject (score < 50)
    Submitted --> UnderReview : Needs review (50-80) / eKYC error
    UnderReview --> Approved : Admin approves
    UnderReview --> Rejected : Admin rejects
    Rejected --> Pending : User updates info/docs
    Approved --> Expired : ExpiresAt reached
    Approved --> Suspended : Admin suspends

    note right of Pending
        User can upload documents,
        update personal info,
        and delete documents
    end note

    note right of Submitted
        VerificationSubmittedEvent fires.
        eKYC handler runs async.
    end note

    note right of UnderReview
        Admin can also update
        verification info directly
    end note
```

## Seller Profile Status Lifecycle

After identity verification is approved, users can create a `SellerProfile`. Its status is managed independently.

```mermaid
---
config:
  layout: elk
---
stateDiagram-v2
    [*] --> Pending : Create seller profile (requires approved verification)
    Pending --> Verified : Admin verifies
    Pending --> Rejected : Admin rejects
    Rejected --> Pending : User updates storeName/storeDescription
    Verified --> Suspended : Admin suspends
```

## End-to-End Seller Onboarding Sequence

```mermaid
sequenceDiagram
    actor User
    participant API as OIO API
    participant DB as Database
    participant Media as Cloudinary
    participant eKYC as VNPT eKYC

    Note over User, eKYC: Phase 1 - Identity Verification
    User->>API: POST /api/me/verifications {verificationType}
    API->>DB: Insert IdentityVerification (status=pending)
    API-->>User: 201 VerificationDto

    User->>API: POST /api/media/upload-signature {context=verification_document}
    API-->>User: UploadSignatureResponse (signature, publicId, folder)
    User->>Media: Upload id_front image with signed params
    Media-->>User: Upload result (secureUrl, publicId)
    User->>API: POST /api/media/confirm {mediaUploadId, publicId, secureUrl, bytes, format}
    API-->>User: ConfirmUploadResponse

    User->>API: POST /api/me/verifications/{id}/documents {mediaUploadId, documentType=id_front}
    API-->>User: 201 VerificationDocumentDto

    Note over User, API: Repeat for id_back, selfie, etc.

    User->>API: PUT /api/me/verifications/{id} {fullName, dateOfBirth, gender, idType, idNumber, ...}
    API->>DB: Update verification info + duplicate check
    API-->>User: 200 VerificationDto

    User->>API: POST /api/me/verifications/{id}/submit
    API->>DB: Status = submitted, raise VerificationSubmittedEvent
    API-->>User: 204 No Content

    Note over API, eKYC: Phase 2 - Async eKYC Processing
    API->>eKYC: Upload images + OCR + card liveness + face compare + face liveness
    eKYC-->>API: EkycVerificationResult (score, decision, ocrData)
    API->>DB: PopulateFromOcr + duplicate check + AutoApprove/AutoReject/MoveToReview
    API->>User: Notification (auto_approved / auto_rejected / manual_review_required)

    Note over User, API: Phase 3 - Seller Profile Creation
    User->>API: POST /api/me/seller-profile {storeName, storeDescription}
    API->>DB: Check approved verification exists, insert SellerProfile (status=pending)
    API-->>User: 201 SellerProfileDto

    Note over API, DB: Phase 4 - Admin Review
    API->>DB: Admin GET /api/admin/seller-profiles
    API->>DB: Admin POST .../verify or .../reject
    API-->>User: Seller profile verified/rejected
```

## eKYC Processing Overview

```mermaid
sequenceDiagram
    participant Handler as VerificationSubmittedEventHandler
    participant DB as Database
    participant VNPT as VNPT eKYC API
    participant DupSvc as DuplicateIdentityService
    participant Notif as NotificationDispatch

    Handler->>DB: Load verification + documents
    Handler->>Handler: Extract id_front, id_back, selfie URLs

    alt Missing id_front or selfie
        Handler->>DB: MoveToReview(score=0)
        Handler->>Notif: Send manual_review_required
    else Documents present
        Handler->>VNPT: POST /file-service/v1/addFile (id_front)
        VNPT-->>Handler: hash (frontHash)
        Handler->>VNPT: POST /file-service/v1/addFile (id_back, optional)
        VNPT-->>Handler: hash (backHash)
        Handler->>VNPT: POST /file-service/v1/addFile (selfie)
        VNPT-->>Handler: hash (selfieHash)

        Handler->>VNPT: POST /ai/v1/ocr/id {frontHash, backHash}
        VNPT-->>Handler: OCR result (name, DOB, idNumber, address, ...)

        Handler->>VNPT: POST /ai/v1/card/liveness {frontHash}
        VNPT-->>Handler: Card liveness result

        Handler->>VNPT: POST /ai/v1/face/compare {frontHash, selfieHash}
        VNPT-->>Handler: Face compare result (prob, MATCH/NOT_MATCH)

        Handler->>VNPT: POST /ai/v1/face/liveness {selfieHash}
        VNPT-->>Handler: Face liveness result

        Handler->>DB: PopulateFromOcr (fullName, DOB, gender, idNumber, address)

        Handler->>DupSvc: FindDuplicateAsync(verification)
        alt Duplicate identity found
            Handler->>DB: AutoReject (rejectionCode=DUPLICATE_IDENTITY)
            Handler->>Notif: Send verification_auto_rejected
        else No duplicate
            alt score >= 80 AND all checks pass
                Handler->>DB: AutoApprove(score, provider, rawResponse)
                Handler->>Notif: Send verification_auto_approved
            else score < 50 OR any check fails
                Handler->>DB: AutoReject(reason, score, provider)
                Handler->>Notif: Send verification_auto_rejected
            else score 50-80
                Handler->>DB: MoveToReview(score, provider)
                Handler->>Notif: Send verification_manual_review_required
            end
        end
    end
```

---

## Subflow Index

| # | File | Description |
|---|------|-------------|
| 01 | [01-create-verification.md](./01-create-verification.md) | Create, update, and query verification requests |
| 02 | [02-upload-documents.md](./02-upload-documents.md) | Document upload flow (signature, Cloudinary, confirm, attach) |
| 03 | [03-submit-ekyc.md](./03-submit-ekyc.md) | Submit for eKYC, VNPT API processing, decision logic |
| 04 | 04-admin-review.md | Admin approve/reject verification (manual review) |
| 05 | 05-seller-profile.md | Create, update, and query seller profiles |
| 06 | 06-admin-seller-profile.md | Admin verify/reject seller profiles |
| 07 | 07-correction-dispute.md | Correction dispute for auto-approved verifications |

---

## Endpoint Reference

### User Verification Endpoints (authenticated user)

| Method | Path | Handler | Permission | Description |
|--------|------|---------|------------|-------------|
| `POST` | `/api/me/verifications` | `CreateVerificationCommand` | `Me.ManageVerification` | Create new verification request |
| `GET` | `/api/me/verifications` | `GetMyVerificationsQuery` | `Me.ReadVerification` | List all my verifications |
| `GET` | `/api/me/verifications/{verificationId}` | `GetMyVerificationByIdQuery` | `Me.ReadVerification` | Get single verification detail |
| `PUT` | `/api/me/verifications/{verificationId}` | `UpdateVerificationCommand` | `Me.ManageVerification` | Update personal info on verification |
| `POST` | `/api/me/verifications/{verificationId}/documents` | `UploadVerificationDocumentCommand` | `Me.ManageVerification` | Attach confirmed media upload as document |
| `DELETE` | `/api/me/verifications/{verificationId}/documents/{docId}` | `DeleteVerificationDocumentCommand` | `Me.ManageVerification` | Remove document from verification |
| `POST` | `/api/me/verifications/{verificationId}/submit` | `SubmitVerificationCommand` | `Me.ManageVerification` | Submit verification for eKYC processing |
| `POST` | `/api/me/verifications/{verificationId}/disputes` | `CreateVerificationCorrectionDisputeCommand` | `Me.ManageVerification` | Open correction dispute for auto-approved verification |

### Admin Verification Endpoints

| Method | Path | Handler | Permission | Description |
|--------|------|---------|------------|-------------|
| `GET` | `/api/admin/verifications` | `GetPendingVerificationsQuery` | `Admin.ReadVerifications` | List pending/under_review verifications |
| `GET` | `/api/admin/verifications/{verificationId}` | `GetVerificationByIdQuery` | `Admin.ReadVerifications` | Get verification detail (admin view) |
| `POST` | `/api/admin/verifications/{verificationId}/approve` | `ApproveVerificationCommand` | `Admin.ManageVerifications` | Approve submitted/under_review verification |
| `POST` | `/api/admin/verifications/{verificationId}/reject` | `RejectVerificationCommand` | `Admin.ManageVerifications` | Reject with reason and optional code |

### Seller Profile Endpoints

| Method | Path | Handler | Permission | Description |
|--------|------|---------|------------|-------------|
| `POST` | `/api/me/seller-profile` | `CreateSellerProfileCommand` | `Me.ManageSellerProfile` | Create seller profile (requires approved verification) |
| `GET` | `/api/me/seller-profile` | `GetMySellerProfileQuery` | `Me.ReadSellerProfile` | Get my seller profile |
| `PUT` | `/api/me/seller-profile` | `UpdateSellerProfileCommand` | `Me.ManageSellerProfile` | Update store name and description |
| `GET` | `/api/admin/seller-profiles` | `GetSellerProfilesQuery` | `Admin.ReadSellerProfiles` | List seller profiles for admin |
| `POST` | `/api/admin/seller-profiles/{id}/verify` | `VerifySellerProfileCommand` | `Admin.ManageSellerProfiles` | Verify (approve) seller profile |
| `POST` | `/api/admin/seller-profiles/{id}/reject` | `RejectSellerProfileCommand` | `Admin.ManageSellerProfiles` | Reject seller profile |

### Media Upload Endpoints (shared)

| Method | Path | Handler | Permission | Description |
|--------|------|---------|------------|-------------|
| `POST` | `/api/media/upload-signature` | `RequestUploadSignatureCommand` | `Media.Upload` | Get signed upload params for Cloudinary |
| `POST` | `/api/media/confirm` | `ConfirmUploadCommand` | `Media.ConfirmUpload` | Confirm upload completed with file metadata |

---

## Domain Events

| Event | Raised By | Consumers | Trigger |
|-------|-----------|-----------|---------|
| `VerificationSubmittedEvent` | `IdentityVerification.Submit()` | `VerificationSubmittedEventHandler` | User submits verification for eKYC |

The handler runs asynchronously via MediatR `INotificationHandler` and performs the full eKYC flow (VNPT API calls, OCR population, duplicate detection, auto-decision).

### Notification Event Types

| Event Type | Trigger | Priority |
|------------|---------|----------|
| `verification_auto_approved` | eKYC score >= 80, all checks pass | Normal |
| `verification_auto_rejected` | eKYC score < 50, check failure, or duplicate identity | Normal |
| `verification_manual_review_required` | Score 50-80, missing docs, or eKYC error | Normal |

---

## Entity & Enum Summary

### Aggregates

| Entity | Key | Description |
|--------|-----|-------------|
| `IdentityVerification` | `IdentityVerificationId` (UUIDv7) | Core verification aggregate with documents, history, and eKYC results |
| `SellerProfile` | `UserId` (same as User PK) | Seller store profile, created after verification approval |

### Child Entities

| Entity | Parent | Description |
|--------|--------|-------------|
| `VerificationDocument` | `IdentityVerification` | Uploaded document (image) linked via `MediaUpload` |
| `VerificationHistory` | `IdentityVerification` | Audit trail of every state change and action |

### Value Objects

| Value Object | Fields | Description |
|--------------|--------|-------------|
| `IdentityDocument` | `idType`, `idNumber`, `issuedDate`, `expiredDate`, `issuedPlace` | Government ID details |
| `PermanentAddress` | `fullAddress`, `province`, `district`, `ward` | Residential address from OCR or manual entry |

### Enums

| Enum | Values | Source File |
|------|--------|------------|
| `IdentityVerificationStatus` | `pending`, `submitted`, `under_review`, `approved`, `rejected`, `expired`, `suspended` | `IdentityVerificationStatus.cs` |
| `SellerProfileStatus` | `pending`, `verified`, `rejected`, `suspended` | `SellerProfileStatus.cs` |
| `VerificationType` | `government_id`, `passport`, `business_owner`, `manual` | `VerificationType.cs` |
| `VerificationDocumentType` | `id_front`, `id_back`, `selfie`, `selfie_with_id`, `business_license`, `bank_statement`, `other` | `VerificationDocumentType.cs` |
| `DocumentVerificationStatus` | `pending`, `valid`, `invalid`, `unclear` | `DocumentVerificationStatus.cs` |
| `VerificationHistoryAction` | `created`, `submitted`, `auto_verified`, `manual_review_started`, `approved`, `rejected`, `resubmitted`, `expired`, `suspended`, `document_uploaded`, `document_deleted`, `info_updated`, `moved_to_review` | `VerificationHistoryAction.cs` |
| `Gender` | `male`, `female`, `other` | `Gender.cs` |
| `IdType` | `cccd`, `cmnd`, `passport` | `IdType.cs` |
| `PerformerType` | `seller`, `buyer`, `admin`, `system` | `PerformerType.cs` |
| `EkycDecision` | `Approved`, `Rejected`, `NeedsReview` | `IEkycProvider.cs` |
