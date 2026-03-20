# 01 - Create & Manage Verification

This document covers the four user-facing endpoints for creating, updating, and querying identity verification requests.

---

## Endpoints

| Method | Path | Command/Query | Response |
|--------|------|---------------|----------|
| `POST` | `/api/me/verifications` | `CreateVerificationCommand` | `201 VerificationDto` |
| `PUT` | `/api/me/verifications/{verificationId}` | `UpdateVerificationCommand` | `200 VerificationDto` |
| `GET` | `/api/me/verifications` | `GetMyVerificationsQuery` | `200 VerificationDto[]` |
| `GET` | `/api/me/verifications/{verificationId}` | `GetMyVerificationByIdQuery` | `200 VerificationDto` |

All endpoints require authentication and the `Me.ManageVerification` or `Me.ReadVerification` permission.

---

## POST - Create Verification

### Request Body

```json
{
  "verificationType": "government_id"
}
```

### Verification Type Enum

| Value | Description |
|-------|-------------|
| `government_id` | Vietnamese CCCD or CMND |
| `passport` | International passport |
| `business_owner` | Business ownership verification |
| `manual` | Manual identity verification |

### Validation

- `verificationType` must be non-empty and one of the allowed values from `VerificationType.All`.

### Business Rules

1. **One pending at a time:** The user cannot have an existing verification in `pending`, `submitted`, or `under_review` status. If found, returns `Verification.AlreadyPending` (409 Conflict).
2. **No duplicate approved type:** The user cannot have an `approved` verification of the same `verificationType`. If found, returns `Verification.AlreadyApproved` (409 Conflict).

### Behavior

1. Validates the request.
2. Checks for existing pending/submitted/under_review verifications for the current user.
3. Checks for existing approved verification of the same type.
4. Creates a new `IdentityVerification` aggregate with:
   - `Id` = new UUIDv7
   - `Status` = `pending`
   - `AttemptCount` = 0
   - History entry: action=`created`, performer=current user, performerType=`buyer`
5. Persists and returns `VerificationDto`.

---

## PUT - Update Verification

### Request Body

```json
{
  "fullName": "Nguyen Van A",
  "dateOfBirth": "1990-01-15",
  "gender": "male",
  "idType": "cccd",
  "idNumber": "012345678901",
  "idIssuedDate": "2020-06-01",
  "idExpiredDate": "2035-06-01",
  "idIssuedPlace": "Cuc CS QLHC ve TTXH",
  "fullAddress": "123 Le Loi, Phuong Ben Thanh",
  "province": "TP Ho Chi Minh",
  "district": "Quan 1",
  "ward": "Phuong Ben Thanh",
  "nationality": "Viet Nam"
}
```

### Field Validation

| Field | Required | Max Length | Validation |
|-------|----------|-----------|------------|
| `fullName` | Yes | 200 | Not whitespace |
| `dateOfBirth` | Yes | - | Valid `DateOnly` |
| `gender` | Yes | - | One of: `male`, `female`, `other` |
| `idType` | Yes | - | One of: `cccd`, `cmnd`, `passport` |
| `idNumber` | Yes | 50 | Not whitespace |
| `idIssuedDate` | No | - | Valid `DateOnly` |
| `idExpiredDate` | No | - | Valid `DateOnly`, must be after `idIssuedDate` |
| `idIssuedPlace` | No | - | Free text |
| `fullAddress` | Yes | 500 | Not whitespace |
| `province` | Yes | 100 | Not whitespace |
| `district` | Yes | 100 | Not whitespace |
| `ward` | Yes | 100 | Not whitespace |
| `nationality` | No | 100 | When present: not whitespace |

### Business Rules

1. **Status guard:** Update is allowed when status is `pending`, `rejected`, or `under_review`. Admin users can also update `approved` verifications.
2. **Rejected to Pending:** If the current status is `rejected`, updating automatically transitions the status back to `pending` and clears `rejectionReason` and `rejectionCode`.
3. **Admin correction of approved:** When an admin updates an `approved` verification, `autoVerified` is set to `false`, and a note "Approved verification corrected by admin" is recorded.
4. **Duplicate identity detection:** After updating, the system checks for duplicate identity documents. If another user has a verification with the same `idType` and normalized `idNumber` in `submitted`, `under_review`, or `approved` status, the update is rejected with `Verification.DuplicateIdentity` (409 Conflict).

### Identity Document Validation

The `IdentityDocument` value object enforces:
- `idNumber` must not be empty.
- If both `issuedDate` and `expiredDate` are provided, `expiredDate` must be after `issuedDate`.

### Behavior

1. Loads verification with documents (ownership check: must belong to current user, or caller is admin).
2. Builds `IdentityDocument` and `PermanentAddress` value objects.
3. Calls `verification.Update(...)` which applies status guard and rejected-to-pending transition.
4. Runs `VerificationDuplicateIdentityService.FindDuplicateAsync()`.
5. Persists and returns updated `VerificationDto`.

---

## GET - List My Verifications

Returns all verifications belonging to the current user, ordered by creation date.

No query parameters.

---

## GET - Get My Verification By ID

Returns a single verification with all documents for the current user.

Path parameter: `verificationId` (GUID).

Returns `404 Verification.NotFound` if not found or does not belong to the current user.

---

## Response Shape: VerificationDto

```json
{
  "id": "019...",
  "userId": "019...",
  "verificationType": "government_id",
  "autoVerified": false,
  "fullName": "Nguyen Van A",
  "dateOfBirth": "1990-01-15",
  "gender": "male",
  "nationality": "Viet Nam",
  "document": {
    "idType": "cccd",
    "idNumber": "012345678901",
    "issuedDate": "2020-06-01",
    "expiredDate": "2035-06-01",
    "issuedPlace": "Cuc CS QLHC ve TTXH"
  },
  "permanentAddress": {
    "fullAddress": "123 Le Loi, Phuong Ben Thanh",
    "province": "TP Ho Chi Minh",
    "district": "Quan 1",
    "ward": "Phuong Ben Thanh"
  },
  "status": "pending",
  "verifiedAt": null,
  "verifiedBy": null,
  "rejectionReason": null,
  "rejectionCode": null,
  "submittedAt": null,
  "expiresAt": null,
  "attemptCount": 0,
  "createdAt": "2026-03-20T10:00:00Z",
  "modifiedAt": null,
  "documents": []
}
```

---

## Duplicate Identity Detection

The `VerificationDuplicateIdentityService` performs duplicate checks at two points:

1. **On update** (`UpdateVerificationCommand`): Prevents saving if duplicate found.
2. **On submit** (`SubmitVerificationCommand`): Prevents submission if duplicate found.
3. **After eKYC** (`VerificationSubmittedEventHandler`): Auto-rejects with `DUPLICATE_IDENTITY` code.

### Matching Logic

- Compares `idType` (exact match) and `idNumber` (normalized: whitespace stripped, uppercased).
- Looks for matches in other users' verifications that are in `submitted`, `under_review`, or `approved` status.
- Excludes verifications belonging to the same user or the same verification ID.

---

## Error Codes

| Code | HTTP | Trigger |
|------|------|---------|
| `Verification.AlreadyPending` | 409 | User has a pending/submitted/under_review verification |
| `Verification.AlreadyApproved` | 409 | User already has an approved verification of this type |
| `Verification.CannotUpdate` | 403 | Update attempted in invalid status |
| `Verification.DuplicateIdentity` | 409 | ID number already used by another account |
| `Verification.NotFound` | 404 | Verification not found or not owned by user |
| `IdentityDocument.IdNumberEmpty` | 422 | Empty ID number in update |
| `IdentityDocument.ExpiredBeforeIssued` | 422 | Expired date is before issued date |
