# Quan Ly Nguoi Dung (User Management)

## Tong quan

Admin co the xem, thay doi trang thai, mo khoa, va xoa nguoi dung. Bao gom ca quan ly seller profile va identity verification.

## Actors

- **Admin** - nguoi co quyen `ManageUsers`

## Endpoint Sequence

### Xem danh sach user
- **Method:** `GET /api/admin/users`
- **Auth:** Required (Permission: `ManageUsers`)
- **Response:** `200 OK` -> Danh sach phan trang

### Xem chi tiet user
- **Method:** `GET /api/admin/users/{userId}`
- **Auth:** Required (Permission: `ManageUsers`)
- **Response:** `200 OK`

### Thay doi trang thai user
- **Method:** `POST /api/admin/users/{userId}/status`
- **Auth:** Required (Permission: `ManageUsers`)
- **Ghi chu:** Thay doi giua Active, Suspended, Banned, ...

### Mo khoa user
- **Method:** `POST /api/admin/users/{userId}/unlock`
- **Auth:** Required (Permission: `ManageUsers`)
- **Ghi chu:** Mo khoa user bi lock do nhap sai mat khau nhieu lan

### Xoa user
- **Method:** `DELETE /api/admin/users/{userId}`
- **Auth:** Required (Permission: `ManageUsers`)
- **Response:** `204 No Content`

## Quan ly Verification

### Xem danh sach cho duyet
- **Method:** `GET /api/admin/verifications`
- **Auth:** Required

### Xem chi tiet verification
- **Method:** `GET /api/admin/verifications/{verificationId}`
- **Auth:** Required

### Duyet verification
- **Method:** `POST /api/admin/verifications/{verificationId}/approve`
- **Auth:** Required

### Tu choi verification
- **Method:** `POST /api/admin/verifications/{verificationId}/reject`
- **Auth:** Required

## Quan ly Seller Profile

### Danh sach seller profiles
- **Method:** `GET /api/admin/seller-profiles`
- **Auth:** Required

### Xac minh seller
- **Method:** `POST /api/admin/seller-profiles/{id}/verify`
- **Auth:** Required

### Tu choi seller
- **Method:** `POST /api/admin/seller-profiles/{id}/reject`
- **Auth:** Required

## Domain Events

- `UserStatusChangedEvent` -> Log va notification
- `UserLockedOutEvent` -> Co the tu dong tao alert

## Luu y nghiep vu

- Thay doi trang thai user anh huong toi khả nang dau gia, ban hang
- User bi Suspended khong the tham gia hoat dong
- User bi Banned vinh vien khong the dang nhap
- Xoa user la soft delete, du lieu duoc giu lai
- Tu dong suspend duoc cau hinh qua `AutoSuspendAfterNonPaymentCount`
