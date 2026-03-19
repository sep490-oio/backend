# Gui eKYC de Duyet (Submit eKYC)

## Tong quan
Sau khi dien day du thong tin va upload tat ca tai lieu can thiet, nguoi dung gui yeu cau xac minh de admin duyet. Verification chuyen tu trang thai `Draft` sang `PendingReview`.

## Actors
- **Nguoi dung da dang nhap** (can quyen `Me.ManageVerification`)

## Endpoint Sequence

### Step 1: Gui yeu cau xac minh

- **Method:** `POST /api/me/verifications/{verificationId}/submit`
- **Auth:** Required - Permission `Me.ManageVerification`
- **Request:** Khong co body (chi can `verificationId` trong URL)
- **Response:** `204 No Content`
- **Loi co the xay ra:**
  - `404 Not Found` - Verification khong ton tai
  - `400 Bad Request` - Verification khong o trang thai `Draft`
  - `400 Bad Request` - Thieu thong tin bat buoc (fullName, dateOfBirth, v.v.)
  - `400 Bad Request` - Thieu tai lieu bat buoc (IdFront, Selfie, v.v.)
  - `403 Forbidden` - Verification khong thuoc user hien tai

## Business Logic (Handler)

1. **Tim verification** theo `verificationId`, include `Documents`
2. **Kiem tra ownership:** Verification phai thuoc user hien tai
3. **Kiem tra trang thai:** Phai o `Draft`
4. **Validate day du:**
   - Thong tin ca nhan: `FullName`, `DateOfBirth`, `Gender`, `IdType`, `IdNumber`, `FullAddress`, `Province`, `District`, `Ward`
   - Tai lieu: Kiem tra cac document type bat buoc theo `VerificationType`
5. **Chuyen trang thai:** `verification.Submit(nowUtc)` -> `PendingReview`
6. **Raise domain event:** `VerificationSubmittedEvent(verificationId, userId, nowUtc)`
7. **Luu:** `SaveChangesAsync`

## Domain Events & Side Effects

| Event | Mo ta |
|-------|-------|
| `VerificationSubmittedEvent` | Phat ra khi verification duoc submit. Handler co the gui notification cho admin |

## Luu y nghiep vu

- **Validation truoc khi submit:** He thong kiem tra tat ca thong tin va tai lieu bat buoc phai duoc dien day du
- **Khong the chinh sua sau khi submit:** Khi verification o trang thai `PendingReview`, user khong the thay doi thong tin hoac tai lieu
- **Thong bao admin:** Event handler gui notification cho admin de biet co verification moi can duyet
- **Sau khi submit:**
  - Neu Admin **Approve**: User duoc xac minh danh tinh
  - Neu Admin **Reject**: User co the tao dispute hoac chinh sua va gui lai
- Mot user co the co nhieu verification (da duyet, bi tu choi, v.v.) nhung chi 1 dang `PendingReview`
