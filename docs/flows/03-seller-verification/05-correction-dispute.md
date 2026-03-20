# 05 - Verification Correction Dispute

## Overview

When a user's identity verification was auto-approved by the eKYC system but contains incorrect personal data (OCR errors), the user can open a correction dispute. This creates a `Dispute` aggregate of type `verification_correction` with `medium` priority, linking the user (complainant) with an admin (respondent) in a threaded conversation.

---

## Endpoint

### POST /api/me/verifications/{verificationId}/disputes

| Property | Value |
|----------|-------|
| Permission | `Me.ManageVerification` |
| Handler | `CreateVerificationCorrectionDisputeCommandHandler` |
| Response | `201 Created` with `DisputeThreadMetaDto` |

---

## Request Body

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `reason` | `string` | Yes | Not whitespace; max 1000 chars |
| `correctedInfo` | `object` | Yes | See nested fields below |
| `message` | `string?` | No | Max 5000 chars |
| `mediaUploadIds` | `Guid[]?` | No | Each must be a valid confirmed upload |

**`correctedInfo` nested fields:**

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `fullName` | `string` | Yes | Not whitespace; max 200 chars |
| `dateOfBirth` | `DateOnly` | Yes | -- |
| `gender` | `string` | Yes | Must be in `Gender.All` set |
| `idType` | `string` | Yes | Must be in `IdType.All` set |
| `idNumber` | `string` | Yes | Not whitespace; max 50 chars |
| `idIssuedDate` | `DateOnly?` | No | -- |
| `idExpiredDate` | `DateOnly?` | No | -- |
| `idIssuedPlace` | `string?` | No | -- |
| `fullAddress` | `string` | Yes | Not whitespace; max 500 chars |
| `province` | `string` | Yes | Not whitespace; max 100 chars |
| `district` | `string` | Yes | Not whitespace; max 100 chars |
| `ward` | `string` | Yes | Not whitespace; max 100 chars |
| `nationality` | `string?` | No | Not whitespace if provided; max 100 chars |

---

## Handler Logic

The `CreateVerificationCorrectionDisputeCommandHandler` executes the following steps:

1. **Load verification** -- Fetch `IdentityVerification` by ID (include `Documents`). Return `Verification.NotFound` if missing or if `UserId` does not match the current user.

2. **Validate auto-approved** -- The verification must be in `approved` status AND have `AutoVerified == true`. Otherwise return `Verification.CorrectionDispute.RequiresAutoApproved`.

3. **Check existing disputes** -- Query the `Dispute` table for any dispute linked to this verification that is not in `resolved`, `closed`, or `cancelled` status. If one exists, return `Verification.CorrectionDispute.AlreadyOpen`.

4. **Find respondent admin** -- Query the `User` table for an active admin (has `Admin` role, not deleted, status = `Active`), ordered by `CreatedAt` ascending (earliest admin). Return `Verification.CorrectionDispute.NoAdminAvailable` if no admin is found.

5. **Load and validate media uploads** (if `mediaUploadIds` provided):
   - Deduplicate IDs.
   - Verify all uploads exist.
   - Verify all belong to the current user.
   - Verify all are confirmed (`IsConfirmed == true`).
   - Verify all have context `dispute_attachment`.
   - Verify none are already linked (`IsLinked == false`).

6. **Create dispute** -- `Dispute.CreateForVerification()` with:
   - Type: `verification_correction`
   - Title: `"KYC correction request"`
   - Description: the `reason` field (trimmed)
   - Priority: `medium`
   - ComplainantId: current user
   - RespondentId: selected admin

7. **Create participant states** -- One for the complainant, one for the respondent admin.

8. **Build initial message** -- A structured text message containing all corrected fields:
   ```
   Verification correction request
   Reason: {reason}

   Corrected information:
   - Full name: {fullName}
   - Date of birth: {dateOfBirth}
   - Gender: {gender}
   - ID type: {idType}
   - ID number: {idNumber}
   - ID issued date: {idIssuedDate}       (if provided)
   - ID expired date: {idExpiredDate}      (if provided)
   - ID issued place: {idIssuedPlace}      (if provided)
   - Full address: {fullAddress}
   - Province: {province}
   - District: {district}
   - Ward: {ward}
   - Nationality: {nationality}            (if provided)

   Additional note:                        (if message provided)
   {message}
   ```

9. **Create attachments** -- For each validated media upload, create a `DisputeMessageAttachment` linked to the initial message and call `upload.LinkToEntity()` + `IMediaRelocationService.RelocateLinkedUploadAsync()`.

10. **Persist** -- Insert dispute, participant states, and attachments. `SaveChangesAsync()`.

11. **Publish event** -- `DisputeMessageSentEvent` with `IsInternal = false`.

12. **Return** -- `DisputeThreadMetaDto` with dispute metadata.

---

## Response DTO

**DisputeThreadMetaDto fields:**

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `Guid` | Dispute identifier |
| `DisputeNumber` | `string` | Human-readable dispute number |
| `Title` | `string` | Always `"KYC correction request"` |
| `Status` | `string` | Initial dispute status |
| `Priority` | `string` | `medium` |
| `AuctionId` | `Guid?` | `null` for verification disputes |
| `VerificationId` | `Guid?` | The linked verification ID |
| `OrderId` | `Guid?` | `null` for verification disputes |
| `ComplainantId` | `Guid` | Current user ID |
| `RespondentId` | `Guid` | Assigned admin ID |
| `AssignedTo` | `Guid?` | Assigned admin if set |
| `CreatedAt` | `DateTime` | Creation timestamp |
| `ResolvedAt` | `DateTime?` | `null` (not yet resolved) |
| `ModifiedAt` | `DateTime?` | Last modification timestamp |

---

## Error Codes

| Code | HTTP Status | Condition |
|------|-------------|-----------|
| `Verification.NotFound` | 404 | Verification does not exist or does not belong to the current user |
| `Verification.CorrectionDispute.RequiresAutoApproved` | 403 | Verification is not auto-approved (must be `approved` status with `AutoVerified = true`) |
| `Verification.CorrectionDispute.AlreadyOpen` | 409 | An open correction dispute already exists for this verification |
| `Verification.CorrectionDispute.NoAdminAvailable` | 409 | No active admin user is available to handle the dispute |
| `Media.NotFound` | 404 | One or more media upload IDs do not exist |
| `Media.NotOwnedByUser` | 403 | One or more uploads belong to a different user |
| `Media.NotConfirm` | 409 | One or more uploads have not been confirmed |
| `Media.WrongContext` | 409 | Upload context is not `dispute_attachment` |
| `Media.AlreadyLinked` | 409 | One or more uploads are already linked to another entity |
