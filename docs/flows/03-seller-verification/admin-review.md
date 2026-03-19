# Admin Duyet Xac minh (Admin Review)

## Tong quan
Admin duyet cac yeu cau xac minh danh tinh (eKYC) cua nguoi dung. Admin co the chap thuan hoac tu choi voi ly do cu the.

## Actors
- **Admin** (can quyen `Admin.ManageVerifications`)

## Endpoint Sequence

### Step 1: Xem danh sach cho duyet

- **Method:** `GET /api/admin/verifications`
- **Auth:** Required - Permission `Admin.ManageVerifications`
- **Query Parameters:**
  ```
  ?status=PendingReview   (loc theo trang thai)
  &page=1
  &pageSize=10
  ```
- **Response:** `200 OK`
  ```json
  {
    "items": [
      {
        "id": "guid",
        "userId": "guid",
        "userName": "string",
        "verificationType": "string",
        "status": "PendingReview",
        "submittedAt": "datetime",
        "createdAt": "datetime"
      }
    ],
    "totalCount": 5,
    "page": 1,
    "pageSize": 10
  }
  ```

### Step 2: Xem chi tiet verification

- **Method:** `GET /api/admin/verifications/{verificationId}`
- **Auth:** Required - Permission `Admin.ManageVerifications`
- **Response:** `200 OK`
  ```json
  {
    "id": "guid",
    "userId": "guid",
    "verificationType": "string",
    "status": "string",
    "fullName": "string",
    "dateOfBirth": "date",
    "gender": "string",
    "idType": "string",
    "idNumber": "string",
    "idIssuedDate": "date?",
    "idExpiredDate": "date?",
    "idIssuedPlace": "string?",
    "fullAddress": "string",
    "province": "string",
    "district": "string",
    "ward": "string",
    "nationality": "string?",
    "documents": [
      {
        "id": "guid",
        "documentType": "string",
        "mediaUrl": "string"
      }
    ],
    "submittedAt": "datetime",
    "createdAt": "datetime"
  }
  ```

### Step 3a: Chap thuan

- **Method:** `POST /api/admin/verifications/{verificationId}/approve`
- **Auth:** Required - Permission `Admin.ManageVerifications`
- **Request:** Khong co body
- **Response:** `204 No Content`
- **Loi co the xay ra:**
  - `404 Not Found` - Verification khong ton tai
  - `400 Bad Request` - Verification khong o trang thai `PendingReview`

### Step 3b: Tu choi

- **Method:** `POST /api/admin/verifications/{verificationId}/reject`
- **Auth:** Required - Permission `Admin.ManageVerifications`
- **Request:**
  ```json
  {
    "reason": "string (required - ly do tu choi)",
    "rejectionCode": "string? (optional - ma tu choi theo danh muc)"
  }
  ```
- **Response:** `204 No Content`
- **Loi co the xay ra:**
  - `404 Not Found` - Verification khong ton tai
  - `400 Bad Request` - Verification khong o trang thai `PendingReview`

## Business Logic

### Approve Handler

1. **Tim verification** theo `verificationId`
2. **Kiem tra trang thai:** Phai o `PendingReview`
3. **Approve:** `verification.Approve(adminId, nowUtc)` -> Trang thai chuyen sang `Approved`
4. **Side effects:**
   - Cap nhat trang thai user: danh dau `IdentityVerified = true`
   - Gan role `Seller` cho user (neu chua co)
5. **Luu:** `SaveChangesAsync`

### Reject Handler

1. **Tim verification** theo `verificationId`
2. **Kiem tra trang thai:** Phai o `PendingReview`
3. **Reject:** `verification.Reject(adminId, reason, rejectionCode, nowUtc)` -> Trang thai chuyen sang `Rejected`
4. **Side effects:**
   - Gui notification cho user ve ly do tu choi
5. **Luu:** `SaveChangesAsync`

## Luu y nghiep vu

- **Admin can doi chieu** thong tin trong verification voi tai lieu da upload (anh CMND/CCCD, selfie)
- **Rejection code** giup phan loai ly do tu choi (vd: `BLURRY_IMAGE`, `INFO_MISMATCH`, `EXPIRED_ID`)
- **Sau khi approve:** User co the bat dau tao san pham dau gia tren platform
- **Sau khi reject:** User co the:
  - Tao **dispute** neu cho rang bi tu choi sai (xem [correction-dispute.md](./correction-dispute.md))
  - Tao verification moi voi thong tin/tai lieu chinh xac hon
- Admin can quyen `Admin.ManageVerifications` de thuc hien cac hanh dong nay
