# 16-05 -- Terms & Conditions

## Overview

Two anonymous endpoints serve the platform's legal documents (terms of service, privacy policy, etc.). Terms are versioned, uploaded as media files, and activated by admins. Only active documents are returned by public endpoints.

---

## Endpoints

### 1. Get All Active Terms

| Property | Value |
|----------|-------|
| Route | `GET api/terms/active` |
| Auth | Anonymous |
| Handler | `GetActiveTermsEndpoint` -> `GetActiveTermsQueryHandler` |
| Response | `200 OK` -- `TermsDocumentDto[]` |

Returns all currently active terms documents across all types.

---

### 2. Get Active Terms by Type

| Property | Value |
|----------|-------|
| Route | `GET api/terms/{type}/active` |
| Auth | Anonymous |
| Handler | `GetActiveTermsByTypeEndpoint` -> `GetActiveTermsByTypeQueryHandler` |
| Response | `200 OK` -- `TermsDocumentDto` |
| Errors | `404 Not Found` -- no active document for this type |

Returns the single active document for a specific term type.

---

## TermType Values

The `TermType` is stored as a string on the `TermsDocument` entity. Typical values include:

| Value | Description |
|-------|-------------|
| `TermsOfService` | Platform terms of service |
| `PrivacyPolicy` | Privacy policy |
| `SellerAgreement` | Seller-specific terms |
| `BuyerAgreement` | Buyer-specific terms |

> Term types are convention-based strings, not a compile-time enum. New types can be added by creating a `TermsDocument` with a new type value.

---

## TermsDocumentDto

```
TermsDocumentDto(
    Guid      Id,
    string    Type,              // e.g. "TermsOfService", "PrivacyPolicy"
    int       Version,
    bool      IsActive,
    DateTime? PublishedAt,       // Set on first activation
    DateTime  CreatedAt,
    string    ContentUrl,        // Public URL to the document file
    string?   FileName,
    long?     FileSize,
    string?   Format,            // e.g. "pdf", "html"
    int?      Width,
    int?      Height,
    double?   DurationSeconds,
    string    StoragePublicId,
    string    StorageFolder
)
```

---

## TermsDocument Entity Lifecycle

1. Admin creates a new terms document via `POST /api/admin/terms` with a media upload
2. Document is created with `IsActive = false` and `Version = 0`
3. Admin activates via `POST /api/admin/terms/{id}/activate`
   - Sets `IsActive = true`
   - Sets `PublishedAt` on first activation
   - Previous active document of the same type is deactivated
4. Public endpoints return only documents where `IsActive = true`

---

## Source Files

| File | Path |
|------|------|
| Endpoint (all active) | `src/presentation/OIO.Api/Endpoints/UserContext/Terms/GetActiveTermsEndpoint.cs` |
| Endpoint (by type) | `src/presentation/OIO.Api/Endpoints/UserContext/Terms/GetActiveTermsByTypeEndpoint.cs` |
| TermsDocumentDto | `src/core/OIO.Application/Context/UserContext/DTOs/TermsDocumentDto.cs` |
| TermsDocument entity | `src/core/OIO.Domain/Context/UserContext/Aggregates/Users/TermsDocument.cs` |
