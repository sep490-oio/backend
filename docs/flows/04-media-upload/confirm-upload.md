# Xac nhan Upload (Confirm Upload)

## Tong quan
Sau khi client upload file len Cloudinary thanh cong, client gui thong tin ket qua len server de xac nhan. Server cap nhat trang thai MediaUpload tu `PendingSignature` sang `Confirmed`.

## Actors
- **Nguoi dung da dang nhap** (can quyen `Media.ConfirmUpload`)

## Endpoint Sequence

### Step 1: Xac nhan upload

- **Method:** `POST /api/media/confirm`
- **Auth:** Required - Permission `Media.ConfirmUpload`
- **Idempotency:** Co `IdempotencyFilter` - gui cung request se tra ve ket qua cached
- **Request:**
  ```json
  {
    "mediaUploadId": "guid (required - ID nhan tu request-signature)",
    "publicId": "string (required - public_id tu Cloudinary response)",
    "secureUrl": "string (required - secure_url tu Cloudinary, phai la URL hop le)",
    "bytes": "long (required - kich thuoc file, > 0)",
    "format": "string (required - dinh dang file, vd: 'jpg', 'png')",
    "fileName": "string? (optional - ten file goc)",
    "width": "int? (optional - chieu rong pixel, cho image/video)",
    "height": "int? (optional - chieu cao pixel, cho image/video)",
    "durationSeconds": "double? (optional - thoi luong, cho video)"
  }
  ```
- **Response:** `201 Created`
  ```json
  {
    "mediaUploadId": "guid",
    "secureUrl": "string",
    "publicId": "string",
    "resourceType": "string"
  }
  ```
- **Loi co the xay ra:**
  - `404 Not Found` - MediaUpload khong ton tai
  - `403 Forbidden` - MediaUpload khong thuoc user hien tai
  - `400 Bad Request` - PublicId khong khop voi expected
  - `409 Conflict` - MediaUpload da duoc confirm truoc do (hoac idempotency)
  - `422 Unprocessable Entity` - Validation loi (URL khong hop le, bytes <= 0, v.v.)

## Business Logic (Handler)

1. **Tim MediaUpload** theo `mediaUploadId`
2. **Kiem tra ownership:** `mediaUpload.UserId == currentUser.UserId`
3. **Kiem tra PublicId:**
   - So sanh `mediaUpload.StorageRef.PublicId` voi `request.PublicId`
   - Ho tro ca so sanh day du va so sanh chi leaf (phan cuoi cua path)
   - `MatchesPublicId(expectedPublicId, actualPublicId)`
4. **Tao MediaInfo:** Cap nhat thong tin media (secureUrl, fileName, bytes, format, width, height, duration)
5. **Confirm:**
   ```
   mediaUpload.Confirm(
     mediaInfo,
     orphanExpirationMinutes: runtimeSettings.Media.OrphanExpiration,
     nowUtc)
   ```
   - Chuyen trang thai sang `Confirmed`
   - Dat timer orphan expiration (neu khong link trong thoi han se bi xoa)
6. **Luu:** `SaveChangesAsync`

## Validation Rules

| Field | Rule |
|-------|------|
| `mediaUploadId` | Khong rong (Guid) |
| `publicId` | Khong rong, phai khop voi expected |
| `secureUrl` | Khong rong, phai la URL tuyet doi hop le |
| `bytes` | > 0 |
| `format` | Khong rong |

## Luu y nghiep vu

- **PublicId matching:** He thong ho tro 2 cach so sanh:
  - Full match: `items/pending/abc123/img_a1b2c3` == `items/pending/abc123/img_a1b2c3`
  - Leaf match: Chi so sanh phan cuoi (`img_a1b2c3`) - phong truong hop Cloudinary thay doi prefix
- **Orphan expiration:** Sau khi confirm, MediaUpload co thoi han de duoc link voi entity (`OrphanExpiration` phut). Neu het han ma chua link -> danh dau orphan
- **Idempotency:** Gui confirm nhieu lan voi cung `mediaUploadId` se tra ve ket qua giong nhau, khong loi
- **Sau khi confirm**, `mediaUploadId` co the duoc su dung de:
  - Link voi Item (`POST /api/items` hoac `POST /api/items/{id}/media`)
  - Link voi Verification Document (`POST /api/me/verifications/{id}/documents`)
  - Link voi User Avatar (`PUT /api/me/profile`)
  - Link voi bat ky entity nao ho tro media
- **Trang thai sau confirm:** `Confirmed` (chua link) -> Can goi endpoint link de su dung
