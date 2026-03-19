# Cap nhat Ho so (Update Profile)

## Tong quan
Nguoi dung cap nhat thong tin ca nhan: ho ten, ten hien thi, ngay sinh, gioi tinh va avatar.

## Actors
- **Nguoi dung da dang nhap** (can quyen `Me.UpdateProfile`)

## Endpoint Sequence

### Step 1: Xem thong tin hien tai

- **Method:** `GET /api/me`
- **Auth:** Required
- **Response:** `200 OK`
  ```json
  {
    "id": "guid",
    "userName": "string",
    "email": "string",
    "status": "string",
    "emailConfirmedAt": "datetime?",
    "phoneNumber": "string?",
    "roles": ["string"]
  }
  ```

### Step 2: Xem profile chi tiet

- **Method:** `GET /api/me/profile`
- **Auth:** Required
- **Response:** `200 OK`
  ```json
  {
    "firstName": "string?",
    "lastName": "string?",
    "displayName": "string?",
    "avatarUrl": "string?",
    "dateOfBirth": "date?",
    "gender": "string?"
  }
  ```

### Step 3: Cap nhat profile

- **Method:** `PUT /api/me/profile`
- **Auth:** Required - Permission `Me.UpdateProfile`
- **Request:**
  ```json
  {
    "firstName": "string? (optional)",
    "lastName": "string? (optional)",
    "displayName": "string? (optional)",
    "avatarMediaUploadId": "guid? (optional - ID tu luong media upload)",
    "dateOfBirth": "date? (optional, format: YYYY-MM-DD)",
    "gender": "string? (optional)"
  }
  ```
- **Response:** `200 OK` (tra ve profile da cap nhat)
- **Loi co the xay ra:**
  - `401 Unauthorized` - Chua dang nhap
  - `403 Forbidden` - Khong co quyen
  - `404 Not Found` - MediaUpload khong ton tai
  - `422 Unprocessable Entity` - Validation loi

## Business Logic (Handler)

1. **Lay user hien tai** include `Profile`
2. **Xu ly avatar** (neu co `avatarMediaUploadId`):
   - Tim `MediaUpload` theo ID
   - Kiem tra ownership (phai thuoc user hien tai)
   - Kiem tra context phai la `user-avatar`
   - Kiem tra trang thai `Confirmed`
   - Link media upload voi user (`upload.LinkTo(entityId)`)
   - Goi `MediaRelocationService` de di chuyen file tu `pending/` sang folder chinh thuc
3. **Cap nhat profile fields:** FirstName, LastName, DisplayName, DateOfBirth, Gender
4. **Luu:** `SaveChangesAsync`

## Luu y nghiep vu

- Avatar su dung luong **Media Upload**: client can goi `POST /api/media/upload-signature` truoc, upload len Cloudinary, confirm, roi truyen `mediaUploadId` vao endpoint nay
- Tat ca cac truong deu optional - chi cap nhat truong nao duoc gui
- `displayName` la ten hien thi tren giao dien, khac voi `userName` (duy nhat, dung de dang nhap)
- `gender` la free-text (co the la "Male", "Female", "Other" hoac bat ky)
- `dateOfBirth` format ISO `YYYY-MM-DD`
