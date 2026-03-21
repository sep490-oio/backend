# 09 - Terms Management

Admin endpoints for creating, listing, and activating terms documents (e.g., Terms of Service, Privacy Policy). Each document is backed by a media upload (PDF) and has a versioned lifecycle.

---

## Endpoints

| # | Method | Route | Description |
|---|--------|-------|-------------|
| 1 | `POST` | `api/admin/terms` | Create a new terms document |
| 2 | `GET` | `api/admin/terms` | List all terms documents |
| 3 | `POST` | `api/admin/terms/{id}/activate` | Activate a terms document |

---

## 1. Create Terms Document

**Request Body:**

```json
{
  "type": "terms_of_service",
  "mediaUploadId": "guid"
}
```

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `type` | `string` | Yes | Not whitespace, max 50 characters. |
| `mediaUploadId` | `Guid` | Yes | Non-empty GUID. Must reference a confirmed, unlinked media upload in a terms upload context. |

**Handler Logic (`CreateTermsDocumentCommandHandler`):**

1. **Load `MediaUpload`** by `mediaUploadId`.
2. **Validate the upload:**
   - Must be owned by the current user.
   - Must be confirmed (`IsConfirmed`).
   - Must not already be linked to another entity (`IsLinked`).
   - Must be in a terms upload context (checked via `UploadContextRegistry.IsTermContext`).
3. **Auto-version:** queries the max existing `Version` for the same `TermType` (case-insensitive) and increments by 1.
4. **Create `TermsDocument`** via `TermsDocument.Create()`, which:
   - Validates the `termType`, `SecureUrl`, `PublicId`, `Folder`, and `version` fields.
   - Copies `StorageRef` and `MediaInfo` from the upload.
   - Links the upload to the new document via `upload.LinkToEntity()`.
   - Sets `IsActive = false` (inactive by default).
5. **Relocate media:** calls `IMediaRelocationService.RelocateLinkedUploadAsync` to move the file to its permanent storage location.
6. **Save and return** `TermsDocumentDto`.

**Response:** `200 OK` with `TermsDocumentDto`.

---

## 2. List All Terms Documents

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `type` | `string?` | Filter by term type (case-insensitive). |
| `isActive` | `bool?` | Filter by active status. |

**Handler Logic (`GetAllTermsDocumentsQueryHandler`):**

- Queries `TermsDocument` set with optional filters.
- Orders by `TermType` ascending, then `Version` descending.
- Returns `IReadOnlyList<TermsDocumentDto>`.

---

## 3. Activate Terms Document

**Route:** `POST api/admin/terms/{id}/activate`

No request body. The `id` is the `TermsDocumentId`.

**Handler Logic (`ActivateTermsDocumentCommandHandler`):**

1. **Load target** `TermsDocument` by ID.
2. **Deactivate siblings:** finds all other `TermsDocument` entities with the same `TermType` that are currently active, and calls `Deactivate()` on each (sets `IsActive = false`).
3. **Activate target:** calls `target.Activate(nowUtc)` which sets `IsActive = true` and `PublishedAt` to `nowUtc` (only on first activation; subsequent activations keep the original `PublishedAt`).
4. **Save changes.**

**Response:** `204 No Content` on success.

---

## TermsDocument Entity

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `TermsDocumentId` | Unique identifier. |
| `TermType` | `string` | Document type (e.g., `"terms_of_service"`, `"privacy_policy"`). |
| `Version` | `int` | Auto-incremented version number per type. |
| `StorageRef` | `StorageRef` | Cloud storage reference (`PublicId`, `Folder`). |
| `Info` | `MediaInfo` | File metadata (`SecureUrl`, `FileName`, `FileSize`, `Format`, etc.). |
| `IsActive` | `bool` | Whether this is the currently active version. |
| `PublishedAt` | `DateTime?` | First activation timestamp. |
| `CreatedAt` | `DateTime` | Creation timestamp. |

---

## TermsDocumentDto

```json
{
  "id": "guid",
  "type": "terms_of_service",
  "version": 3,
  "isActive": true,
  "publishedAt": "2026-03-21T00:00:00Z",
  "createdAt": "2026-03-20T10:00:00Z",
  "contentUrl": "https://...",
  "fileName": "tos-v3.pdf",
  "fileSize": 245000,
  "format": "pdf",
  "width": null,
  "height": null,
  "durationSeconds": null,
  "storagePublicId": "terms/tos-v3",
  "storageFolder": "terms"
}
```

---

## Key Source Files

| File | Path |
|------|------|
| Create command | `src/core/OIO.Application/Context/UserContext/Commands/CreateTermsDocument/CreateTermsDocumentCommand.cs` |
| Create handler | `src/core/OIO.Application/Context/UserContext/Commands/CreateTermsDocument/CreateTermsDocumentCommandHandler.cs` |
| Activate command + handler | `src/core/OIO.Application/Context/UserContext/Commands/ActivateTermsDocument/ActivateTermsDocumentCommand.cs` + `Handler.cs` |
| List query + handler | `src/core/OIO.Application/Context/UserContext/Queries/GetAllTermsDocuments/GetAllTermsDocumentsQuery.cs` + `Handler.cs` |
| Domain entity | `src/core/OIO.Domain/Context/UserContext/Aggregates/Users/TermsDocument.cs` |
| DTO | `src/core/OIO.Application/Context/UserContext/DTOs/TermsDocumentDto.cs` |
