# Khieu nai / Sua Thong tin (Correction Dispute)

## Tong quan
Khi verification bi tu choi, nguoi dung co the tao khieu nai (dispute) kem theo thong tin da chinh sua va tai lieu bo sung. Dispute tao ra mot thread trao doi giua user va admin.

## Actors
- **Nguoi dung da dang nhap** (can quyen `Me.ManageVerification`)
- **Admin** (xu ly dispute)

## Endpoint Sequence

### Step 1: Tao khieu nai

- **Method:** `POST /api/me/verifications/{verificationId}/disputes`
- **Auth:** Required - Permission `Me.ManageVerification`
- **Request:**
  ```json
  {
    "reason": "string (required - ly do khieu nai)",
    "correctedInfo": {
      "fullName": "string (required)",
      "dateOfBirth": "date (required)",
      "gender": "string (required)",
      "idType": "string (required)",
      "idNumber": "string (required)",
      "idIssuedDate": "date? (optional)",
      "idExpiredDate": "date? (optional)",
      "idIssuedPlace": "string? (optional)",
      "fullAddress": "string (required)",
      "province": "string (required)",
      "district": "string (required)",
      "ward": "string (required)",
      "nationality": "string? (optional)"
    },
    "message": "string? (optional - tin nhan bo sung cho admin)",
    "mediaUploadIds": ["guid"] // optional - tai lieu bo sung (da upload qua Media Upload flow)
  }
  ```
- **Response:** `201 Created`
  ```json
  {
    "disputeId": "guid",
    "verificationId": "guid",
    "status": "Open",
    "createdAt": "datetime"
  }
  ```
- **Loi co the xay ra:**
  - `404 Not Found` - Verification khong ton tai
  - `400 Bad Request` - Verification khong o trang thai `Rejected`
  - `409 Conflict` - Da co dispute dang mo cho verification nay
  - `401 Unauthorized` - Chua dang nhap
  - `403 Forbidden` - Verification khong thuoc user hien tai

## Business Logic (Handler)

1. **Tim verification** theo `verificationId`, include documents
2. **Kiem tra ownership:** Verification phai thuoc user hien tai
3. **Kiem tra trang thai:** Verification phai o `Rejected`
4. **Kiem tra trung lap:** Khong cho phep tao dispute moi khi da co dispute dang `Open`
5. **Xu ly media** (neu co `mediaUploadIds`):
   - Tim cac `MediaUpload` theo IDs
   - Kiem tra ownership, context, trang thai confirmed, chua bi linked
   - Link media voi dispute
   - Goi `MediaRelocationService` de di chuyen file
6. **Tao dispute thread:** Tao `DisputeThread` moi voi:
   - `Reason`: ly do khieu nai
   - `CorrectedInfo`: thong tin da chinh sua
   - `Message`: tin nhan dau tien (neu co)
   - Tai lieu bo sung (neu co)
7. **Chuyen trang thai verification:** `Disputed`
8. **Luu:** `SaveChangesAsync`

## Luong xu ly Dispute (Admin)

Admin xu ly dispute qua cac endpoint dispute chung:

- `GET /api/disputes/{disputeId}` - Xem chi tiet dispute
- `GET /api/disputes/{disputeId}/messages` - Xem tin nhan
- `POST /api/disputes/{disputeId}/messages` - Gui tin nhan (trao doi voi user)
- `POST /api/admin/disputes/{disputeId}/resolve` - Ket thuc dispute

## Luu y nghiep vu

- **CorrectedInfo** la thong tin da duoc user sua lai, admin se doi chieu voi thong tin cu va tai lieu moi
- **Dispute thread** cho phep admin va user trao doi qua tin nhan (giong chat)
- **Media bo sung** co the la anh chup lai ro hon, tai lieu khac, v.v.
- Sau khi admin resolve dispute:
  - Neu dong y: Admin co the approve verification
  - Neu khong dong y: Verification van giu trang thai `Rejected`, user co the tao verification moi
- **MediaUploadIds** phai la cac file da upload va confirm qua luong Media Upload, voi context phu hop
- Moi dispute tao ra mot thread rieng, khong anh huong den cac verification khac
