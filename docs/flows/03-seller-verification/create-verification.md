# Tao Yeu cau Xac minh (Create Verification)

## Tong quan
Nguoi dung bat dau quy trinh xac minh danh tinh bang cach tao yeu cau xac minh moi. Yeu cau duoc tao o trang thai `Draft`.

## Actors
- **Nguoi dung da dang nhap** (can quyen `Me.ManageVerification`)

## Endpoint Sequence

### Step 1: Tao yeu cau xac minh

- **Method:** `POST /api/me/verifications`
- **Auth:** Required - Permission `Me.ManageVerification`
- **Request:**
  ```json
  {
    "verificationType": "string (required - 'IdentityCard', 'Passport', 'DriverLicense')"
  }
  ```
- **Response:** `201 Created`
  ```json
  {
    "id": "guid",
    "verificationType": "string",
    "status": "Draft",
    "createdAt": "datetime"
  }
  ```
- **Loi co the xay ra:**
  - `409 Conflict` - Da co verification dang `PendingReview` hoac `Draft`
  - `401 Unauthorized` - Chua dang nhap
  - `403 Forbidden` - Khong co quyen

### Step 2: Dien thong tin xac minh

- **Method:** `PUT /api/me/verifications/{verificationId}`
- **Auth:** Required - Permission `Me.ManageVerification`
- **Request:**
  ```json
  {
    "fullName": "string (required - ho ten day du)",
    "dateOfBirth": "date (required - YYYY-MM-DD)",
    "gender": "string (required - Male/Female/Other)",
    "idType": "string (required - loai giay to)",
    "idNumber": "string (required - so giay to)",
    "idIssuedDate": "date? (optional - ngay cap)",
    "idExpiredDate": "date? (optional - ngay het han)",
    "idIssuedPlace": "string? (optional - noi cap)",
    "fullAddress": "string (required - dia chi day du)",
    "province": "string (required)",
    "district": "string (required)",
    "ward": "string (required)",
    "nationality": "string? (optional, default theo he thong)"
  }
  ```
- **Response:** `200 OK`
- **Loi co the xay ra:**
  - `404 Not Found` - Verification khong ton tai
  - `400 Bad Request` - Verification khong o trang thai `Draft`
  - `403 Forbidden` - Verification khong thuoc user hien tai

### Step 3: Xem danh sach xac minh cua toi

- **Method:** `GET /api/me/verifications`
- **Auth:** Required - Permission `Me.ManageVerification`
- **Response:** `200 OK` (danh sach phan trang)

### Step 4: Xem chi tiet xac minh

- **Method:** `GET /api/me/verifications/{verificationId}`
- **Auth:** Required - Permission `Me.ManageVerification`
- **Response:** `200 OK`

## Luu y nghiep vu

- **Verification type** xac dinh loai giay to duoc su dung: CMND/CCCD (`IdentityCard`), Ho chieu (`Passport`), Bang lai xe (`DriverLicense`)
- **Trang thai `Draft`**: User co the tu do chinh sua thong tin va upload tai lieu
- Chi cho phep 1 verification `Draft` hoac `PendingReview` tai 1 thoi diem
- Sau khi tao, user can **dien thong tin** (Step 2) va **upload tai lieu** (xem [upload-documents.md](./upload-documents.md)) truoc khi gui duyet
- Thong tin ca nhan phai khop voi giay to upload (admin se doi chieu)
