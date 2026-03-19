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

### Tao user moi (Admin)
- **Method:** `POST /api/admin/users`
- **Auth:** Required (Permission: `ManageUsers`)
- **Request:**
  ```json
  {
    "userName": "string (required)",
    "email": "string (required)",
    "password": "string (tuy chon - neu khong truyen, he thong tu tao temporary password 12 ky tu)",
    "currency": "string (required - VND, USD, ...)",
    "firstName": "string (required)",
    "lastName": "string (required)",
    "displayName": "string (tuy chon - mac dinh = userName)",
    "roles": ["Bidder", "User", "..."] ,
    "emailConfirmed": false,
    "skipNotifications": false
  }
  ```
- **Response:** `201 Created`
  ```json
  {
    "userId": "guid",
    "userName": "string",
    "email": "string",
    "status": "string",
    "roles": ["Bidder", "User"],
    "emailConfirmed": false,
    "temporaryPassword": "string (chi tra ve khi he thong tu generate)"
  }
  ```
- **Business Logic:**
  - Neu `password` khong truyen: tu dong tao mat khau tam 12 ky tu (bao gom uppercase, lowercase, so, ky tu dac biet)
  - Neu `roles` khong truyen: mac dinh gan `Bidder` + `User`
  - **Role escalation prevention:** Admin chi co the gan role co level thap hon level cao nhat cua chinh minh. Neu admin co `maxRoleLevel = 5`, khong the gan role co `level >= 5`
  - Kiem tra trung `email` va `userName` truoc khi tao
  - Neu `emailConfirmed = true`: tu dong confirm email, khong can OTP
  - Neu `skipNotifications = true`: xoa domain events, khong gui email chao mung
  - `temporaryPassword` chi xuat hien trong response khi he thong tu generate (khong luu plaintext)

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
