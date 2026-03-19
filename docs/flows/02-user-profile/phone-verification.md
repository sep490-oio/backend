# Xac thuc So Dien thoai (Phone Verification)

## Tong quan
Nguoi dung dang ky va xac thuc so dien thoai. Luong gom 2 buoc: dat so dien thoai (gui OTP) va xac thuc bang ma OTP.

## Actors
- **Nguoi dung da dang nhap** (can quyen `Me.ManagePhone`)

## Endpoint Sequence

### Step 1: Dat so dien thoai

- **Method:** `PUT /api/me/phone`
- **Auth:** Required - Permission `Me.ManagePhone`
- **Request:**
  ```json
  {
    "phoneNumber": "string (required - so dien thoai, vd: '0912345678')",
    "countryCode": "string? (optional, default: 'VN')"
  }
  ```
- **Response:** `204 No Content`
- **Loi co the xay ra:**
  - `400 Bad Request` - So dien thoai khong hop le
  - `401 Unauthorized` - Chua dang nhap
  - `403 Forbidden` - Khong co quyen
  - `409 Conflict` - So dien thoai da duoc dang ky boi user khac
  - `422 Unprocessable Entity` - Validation loi

### Step 2: Xac thuc bang ma OTP

- **Method:** `POST /api/me/phone/confirm`
- **Auth:** Required - Permission `Me.ManagePhone`
- **Request:**
  ```json
  {
    "verificationCode": "string (required - ma OTP nhan duoc)"
  }
  ```
- **Response:** `204 No Content`
- **Loi co the xay ra:**
  - `400 Bad Request` - Ma OTP sai hoac het han
  - `401 Unauthorized` - Chua dang nhap
  - `403 Forbidden` - Khong co quyen
  - `422 Unprocessable Entity` - Validation loi

## Business Logic

### Set Phone Number Handler

1. **Lay user hien tai** tu `ICurrentUser`
2. **Validate so dien thoai:** `PhoneNumber.Create(phoneNumber, countryCode)` - Parse va validate bang thu vien phone number (libphonenumber)
3. **Kiem tra trung lap:** Tim xem so dien thoai da duoc dang ky boi user khac chua
4. **Luu so dien thoai (chua xac thuc):** Cap nhat `user.PhoneNumber` (trang thai pending)
5. **Gui OTP:** Tao ma OTP va gui qua SMS
6. **Luu:** `SaveChangesAsync`

### Confirm Phone Number Handler

1. **Lay user hien tai**
2. **Validate OTP:** Kiem tra ma OTP co dung va con han khong
3. **Xac thuc:** `user.ConfirmPhoneNumber(nowUtc)` - Danh dau so dien thoai da xac thuc
4. **Luu:** `SaveChangesAsync`

## Domain Events & Side Effects

| Event | Mo ta |
|-------|-------|
| `UserPhoneConfirmedEvent` | Phat ra khi so dien thoai duoc xac thuc thanh cong |

## Luu y nghiep vu

- **Country code** mac dinh la `VN` (Viet Nam, +84)
- So dien thoai duoc normalize (loai bo khoang trang, dau `-`, chuyen ve dang quoc te)
- OTP co thoi han (thuong 5-10 phut)
- Sau khi xac thuc, so dien thoai co the duoc su dung cho 2FA (SMS), thong bao, va van chuyen
- Thay doi so dien thoai yeu cau xac thuc lai (buoc 1 + buoc 2)
- So dien thoai da xac thuc la dieu kien de tao Seller Profile trong mot so truong hop
