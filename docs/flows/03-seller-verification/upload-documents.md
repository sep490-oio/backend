# Upload Tai lieu Xac minh (Upload Verification Documents)

## Tong quan
Nguoi dung upload cac tai lieu xac minh danh tinh (anh mat truoc/mat sau giay to, selfie, chung minh dia chi). Su dung luong Media Upload de upload file len Cloudinary truoc, sau do lien ket voi verification.

## Actors
- **Nguoi dung da dang nhap** (can quyen `Me.ManageVerification`)

## Endpoint Sequence

### Tien quyet: Upload file len Cloudinary

Truoc khi lien ket tai lieu, user can upload file qua luong **Media Upload** (xem [04-media-upload](../04-media-upload/README.md)):

1. `POST /api/media/upload-signature` (context: `verification`) -> Nhan `mediaUploadId` + signature
2. Upload truc tiep len Cloudinary voi signature
3. `POST /api/media/confirm` -> Xac nhan upload thanh cong

### Step 1: Lien ket tai lieu voi verification

- **Method:** `POST /api/me/verifications/{verificationId}/documents`
- **Auth:** Required - Permission `Me.ManageVerification`
- **Request:**
  ```json
  {
    "mediaUploadId": "guid (required - ID tu buoc upload truoc)",
    "documentType": "string (required - 'IdFront', 'IdBack', 'Selfie', 'ProofOfAddress')"
  }
  ```
- **Response:** `201 Created`
  ```json
  {
    "id": "guid (document ID)",
    "documentType": "string",
    "mediaUrl": "string",
    "createdAt": "datetime"
  }
  ```
- **Loi co the xay ra:**
  - `404 Not Found` - Verification hoac MediaUpload khong ton tai
  - `400 Bad Request` - Verification khong o trang thai `Draft`
  - `409 Conflict` - Tai lieu loai nay da duoc upload
  - `403 Forbidden` - Verification hoac MediaUpload khong thuoc user hien tai

### Step 2: Xoa tai lieu (neu can upload lai)

- **Method:** `DELETE /api/me/verifications/{verificationId}/documents/{docId}`
- **Auth:** Required - Permission `Me.ManageVerification`
- **Response:** `204 No Content`
- **Loi co the xay ra:**
  - `404 Not Found` - Tai lieu khong ton tai
  - `400 Bad Request` - Verification khong o trang thai cho phep xoa

## Business Logic (Upload Document Handler)

1. **Tim verification** theo `verificationId`, kiem tra thuoc user hien tai
2. **Kiem tra trang thai:** Verification phai o `Draft`
3. **Tim MediaUpload** theo `mediaUploadId`:
   - Kiem tra ton tai
   - Kiem tra ownership (thuoc user hien tai)
   - Kiem tra context phai la `verification`
   - Kiem tra trang thai `Confirmed` (da upload thanh cong)
   - Kiem tra chua bi linked
4. **Lien ket media:** `upload.LinkTo(verificationId)` - Danh dau media da duoc su dung
5. **Them document:** Tao `VerificationDocument` voi `documentType` va thong tin media
6. **Relocation:** Goi `IMediaRelocationService.RelocateLinkedUploadAsync(upload)` - Di chuyen file tu `pending/` sang `verifications/{verificationId}/`
7. **Luu:** `SaveChangesAsync`

## Luu y nghiep vu

- **Document types** can thiet tuy theo `verificationType`:
  - `IdentityCard`: `IdFront` (bat buoc), `IdBack` (bat buoc), `Selfie` (bat buoc)
  - `Passport`: `IdFront` (bat buoc), `Selfie` (bat buoc)
  - `DriverLicense`: `IdFront` (bat buoc), `IdBack` (bat buoc), `Selfie` (bat buoc)
- **ProofOfAddress** la tuy chon, co the yeu cau trong truong hop dac biet
- File upload phai qua luong **Media Upload** - khong ho tro upload truc tiep
- Sau khi lien ket, file se duoc **relocate** tu folder tam (`pending/`) sang folder chinh thuc (`verifications/{id}/`)
- User co the xoa tai lieu va upload lai khi verification con o trang thai `Draft`
- Moi loai document chi cho phep 1 file (upload moi se thay the cu)
