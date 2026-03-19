# OIO Auction Platform - Flow Documentation

Tai lieu mo ta chi tiet cac luong nghiep vu (business flows) cua he thong dau gia OIO.
Moi flow bao gom: endpoint sequence, request/response, business rules, domain events va side effects.

## Muc luc

### 01 - Registration & Authentication
Luong dang ky, xac thuc va quan ly phien dang nhap.

- [Tong quan](./01-registration-auth/README.md)
- [Dang ky tai khoan](./01-registration-auth/registration.md)
- [Xac thuc email](./01-registration-auth/email-verification.md)
- [Dang nhap](./01-registration-auth/login.md)
- [Lam moi token](./01-registration-auth/refresh-token.md)
- [Quen & dat lai mat khau](./01-registration-auth/forgot-reset-password.md)
- [Xac thuc hai yeu to - TOTP 2FA](./01-registration-auth/two-factor-auth.md) (Setup, Confirm, Login verify, Recovery codes)
- [Quan ly phien dang nhap](./01-registration-auth/session-management.md)

### 02 - User Profile
Quan ly thong tin ca nhan, so dien thoai, dia chi cua nguoi dung.

- [Tong quan](./02-user-profile/README.md)
- [Cap nhat ho so](./02-user-profile/update-profile.md)
- [Xac thuc so dien thoai](./02-user-profile/phone-verification.md)
- [Quan ly dia chi](./02-user-profile/address-management.md)
- [Ho so nguoi ban](./02-user-profile/seller-profile.md)

### 03 - Seller Verification (eKYC)
Luong xac minh danh tinh nguoi ban (Identity Verification / eKYC).

- [Tong quan](./03-seller-verification/README.md)
- [Tao yeu cau xac minh](./03-seller-verification/create-verification.md)
- [Upload tai lieu](./03-seller-verification/upload-documents.md)
- [Gui eKYC](./03-seller-verification/submit-ekyc.md)
- [Admin duyet](./03-seller-verification/admin-review.md)
- [Khieu nai / Sua thong tin](./03-seller-verification/correction-dispute.md)

### 04 - Media Upload
Luong upload media (hinh anh, video) qua Cloudinary voi signed upload.

- [Tong quan](./04-media-upload/README.md)
- [Yeu cau chu ky upload](./04-media-upload/request-signature.md)
- [Client upload len Cloudinary](./04-media-upload/client-upload.md)
- [Xac nhan upload](./04-media-upload/confirm-upload.md)
- [Background relocation & cleanup](./04-media-upload/background-relocation-cleanup.md)

### 05 - Item Management
Quan ly san pham dau gia: tao, gan media, gui duyet, admin review, kich hoat.

- [Tong quan](./05-item-management/README.md)
- [Tao san pham](./05-item-management/create-item.md)
- [Quan ly media cua san pham](./05-item-management/manage-media.md)
- [Gui duyet san pham](./05-item-management/submit-for-review.md)
- [Admin duyet san pham](./05-item-management/admin-review.md)
- [Kich hoat san pham](./05-item-management/activate-item.md)
- [Giao hang san pham](./05-item-management/item-shipping.md)
- [Kiem tra chat luong (QA)](./05-item-management/item-qa.md)

---

## Quy uoc

| Ky hieu | Y nghia |
|---------|---------|
| `Required` | Bat buoc |
| `Optional` | Tuy chon |
| `Auth: Required` | Can dang nhap (Bearer token) |
| `Auth: Anonymous` | Khong can dang nhap |
| `Permission: X` | Can quyen X |

## Kien truc tong quan

```
Client (SPA / Mobile)
    |
    v
[API Gateway / Endpoints]  -->  [MediatR Pipeline]  -->  [Command/Query Handlers]
    |                              |                           |
    |                         Validation                  Domain Logic
    |                         Behavior                    (Aggregates, VOs, Events)
    |                              |                           |
    |                              v                           v
    |                        [Domain Events]           [EF Core / DbContext]
    |                              |
    |                              v
    |                   [Event Handlers / Side Effects]
    |                   (Email, Notification, Jobs...)
    v
[Background Jobs]
  - ExpiredSessionCleanupJob (moi 6h)
  - PendingUploadRelocationJob (Quartz)
  - ScanActiveAuctionsForCollusionJob
```
