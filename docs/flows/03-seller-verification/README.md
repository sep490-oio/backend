# 03 - Seller Verification (eKYC)

## Tong quan

Module xac minh danh tinh nguoi ban (Identity Verification / eKYC). Nguoi dung tao yeu cau xac minh, upload tai lieu, dien thong tin ca nhan, gui de admin duyet. Admin co the chap thuan hoac tu choi. Nguoi dung co the khieu nai neu bi tu choi sai.

## State Machine

```
[Chua tao]
    |
    | POST /api/me/verifications
    v
[Draft]
    |
    +-- PUT  (cap nhat thong tin)
    +-- POST (upload tai lieu)
    +-- DELETE (xoa tai lieu)
    |
    | POST /api/me/verifications/{id}/submit
    v
[PendingReview]
    |
    +-- Admin Approve --> [Approved] --> User duoc xac minh
    |
    +-- Admin Reject --> [Rejected]
                            |
                            +-- User Dispute --> [Disputed]
                            |                       |
                            |                       +-- Admin resolve dispute
                            |
                            +-- User Update & Resubmit --> [PendingReview]
```

## Cac subflow

| # | Subflow | File |
|---|---------|------|
| 1 | Tao yeu cau xac minh | [create-verification.md](./create-verification.md) |
| 2 | Upload tai lieu | [upload-documents.md](./upload-documents.md) |
| 3 | Gui eKYC | [submit-ekyc.md](./submit-ekyc.md) |
| 4 | Admin duyet | [admin-review.md](./admin-review.md) |
| 5 | Khieu nai / Sua thong tin | [correction-dispute.md](./correction-dispute.md) |

## Endpoint Map

### User Endpoints

| Method | URL | Mo ta |
|--------|-----|-------|
| `POST` | `/api/me/verifications` | Tao yeu cau xac minh |
| `GET` | `/api/me/verifications` | Xem danh sach xac minh |
| `GET` | `/api/me/verifications/{verificationId}` | Xem chi tiet xac minh |
| `PUT` | `/api/me/verifications/{verificationId}` | Cap nhat thong tin |
| `POST` | `/api/me/verifications/{verificationId}/documents` | Upload tai lieu |
| `DELETE` | `/api/me/verifications/{verificationId}/documents/{docId}` | Xoa tai lieu |
| `POST` | `/api/me/verifications/{verificationId}/submit` | Gui de duyet |
| `POST` | `/api/me/verifications/{verificationId}/disputes` | Tao khieu nai |

### Admin Endpoints

| Method | URL | Mo ta |
|--------|-----|-------|
| `GET` | `/api/admin/verifications` | Danh sach cho duyet |
| `GET` | `/api/admin/verifications/{verificationId}` | Chi tiet xac minh |
| `POST` | `/api/admin/verifications/{verificationId}/approve` | Chap thuan |
| `POST` | `/api/admin/verifications/{verificationId}/reject` | Tu choi |

## Domain Events

| Event | Mo ta |
|-------|-------|
| `VerificationSubmittedEvent` | Khi user gui yeu cau xac minh de duyet |

## Luu y nghiep vu

- Moi user chi co the co 1 verification dang `PendingReview` tai 1 thoi diem
- Tai lieu upload su dung luong **Media Upload** (xem [04-media-upload](../04-media-upload/README.md))
- Verification type: `IdentityCard`, `Passport`, `DriverLicense`
- Document type: `IdFront`, `IdBack`, `Selfie`, `ProofOfAddress`
- Tat ca endpoint user can quyen `Me.ManageVerification`
- Tat ca endpoint admin can quyen `Admin.ManageVerifications`
