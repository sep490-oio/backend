# Yeu cau Chu ky Upload (Request Upload Signature)

## Tong quan
Client yeu cau server tao chu ky (signature) de upload file truc tiep len Cloudinary. Server tra ve tat ca thong tin can thiet cho client thuc hien signed upload.

## Actors
- **Nguoi dung da dang nhap** (can quyen `Media.Upload`)

## Endpoint Sequence

### Step 1: Yeu cau chu ky

- **Method:** `POST /api/media/upload-signature`
- **Auth:** Required - Permission `Media.Upload`
- **Idempotency:** Co `IdempotencyFilter` - gui cung request se tra ve ket qua cached
- **Request:**
  ```json
  {
    "context": "string (required - vd: 'item-image', 'verification', 'user-avatar')",
    "fileName": "string (required - ten file goc)"
  }
  ```
- **Response:** `200 OK`
  ```json
  {
    "mediaUploadId": "guid (ID de dung khi confirm va link)",
    "uploadUrl": "string (URL de upload len Cloudinary)",
    "signature": "string (chu ky Cloudinary)",
    "timestamp": "long (timestamp tao chu ky)",
    "apiKey": "string (Cloudinary API key)",
    "cloudName": "string (Cloudinary cloud name)",
    "publicId": "string (public_id cho upload)",
    "storagePublicId": "string (public_id luu trong DB)",
    "folder": "string (folder tren Cloudinary)",
    "eager": "string? (Cloudinary eager transformation)",
    "resourceType": "string ('image', 'video', 'raw')",
    "maxFileSize": "long (kich thuoc toi da, bytes)",
    "allowedFormats": ["string (cac dinh dang cho phep)"]
  }
  ```
- **Loi co the xay ra:**
  - `400 Bad Request` - Context khong hop le
  - `409 Conflict` - Request trung lap (idempotency)
  - `422 Unprocessable Entity` - Validation loi

### Step (tham khao): Lay danh sach contexts

- **Method:** `GET /api/media/contexts`
- **Auth:** Required - Permission `Media.ReadContexts`
- **Response:** `200 OK` - Danh sach tat ca upload contexts va cau hinh

## Business Logic (Handler)

1. **Validate context:** Kiem tra `context` co trong `UploadContextRegistry` khong
2. **Lay cau hinh context:** `contextRegistry.Get(context)` -> `ContextConfig` (folder, resourceType, maxSize, allowedFormats, eager)
3. **Tao folder tam:** `{contextConfig.Folder}/pending/{userId}` (vd: `items/pending/abc123`)
4. **Tao media name:** `{prefix}_{uniqueSuffix}` (vd: `img_a1b2c3d4e5f6`)
5. **Tao Cloudinary signature:**
   ```
   signatureService.GenerateSignature(
     resourceType, mediaName, folder, eager, allowedFormats)
   ```
6. **Tao MediaUpload entity:**
   ```
   MediaUpload.Create(
     userId, context, resourceType, entityId: null, idType: null,
     mediaInfo, storageRef, nowUtc, signatureExpirationMinutes)
   ```
7. **Luu vao DB:** Insert `MediaUpload` voi trang thai `PendingSignature`
8. **Tra ve response:** Bao gom tat ca thong tin de client upload

## Luu y nghiep vu

- **Signature co thoi han:** Duoc cau hinh qua `RuntimeSettings.Media.SignatureExpiration` (tinh bang phut)
- **PublicId duoc server sinh:** Client KHONG duoc tu chon public_id, dam bao unique va bao mat
- **Folder tam (`pending/`):** File upload vao day, chi duoc di chuyen khi MediaUpload duoc link voi entity
- **ResourceType:** Xac dinh boi context (`image`, `video`, `raw`). Client phai upload dung loai file
- **AllowedFormats:** Server chi dinh cac format cho phep (vd: jpg, png, webp cho item-image)
- **MaxFileSize:** Client nen validate truoc khi upload, Cloudinary cung co the reject neu vuot qua
- **Idempotency:** Gui cung request (context + fileName) nhieu lan se tra ve cung ket qua, khong tao MediaUpload moi
- **mediaUploadId** la key de su dung trong cac buoc tiep theo (confirm, link voi entity)
