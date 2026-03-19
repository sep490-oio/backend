# Xac thuc Hai Yeu to - TOTP (Two-Factor Authentication)

## Tong quan

He thong ho tro 2FA bang TOTP (Time-based One-Time Password) tuong thich voi Google Authenticator, Authy, va cac app tuong tu. Flow gom 3 giai doan: Setup (tao secret + QR code), Confirm (xac nhan user co the tao ma), va Login verification (buoc 2 khi dang nhap).

## Actors
- **Nguoi dung da dang nhap** (can quyen `ManageTwoFactor`) — cho setup/disable
- **Nguoi dung dang nhap** (co limited 2FA JWT) — cho verify login

## Provider ho tro

| Provider | Trang thai | Mo ta |
|----------|-----------|-------|
| `totp` | **Implemented** | TOTP qua Authenticator app (Google Authenticator, Authy) |
| `sms` | Placeholder | SMS OTP (chua implement) |
| `email` | Placeholder | Email OTP (chua implement) |

---

## Flow 1: Setup TOTP (Bat 2FA)

### Step 1: Khoi tao TOTP Setup

- **Method:** `POST /api/me/two-factor/setup`
- **Auth:** Required — Permission `ManageTwoFactor`
- **Request:** Khong co body
- **Response:** `200 OK`
  ```json
  {
    "sharedKey": "JBSWY3DPEHPK3PXP...",
    "qrCodeBase64": "iVBORw0KGgo..."
  }
  ```
- **Ghi chu:**
  - `sharedKey`: Base32 secret de nhap thu cong vao app
  - `qrCodeBase64`: QR code PNG encoded base64 — frontend hien thi bang `<img src="data:image/png;base64,{qrCodeBase64}">`
  - Secret duoc luu vao `PendingTwoFactorSecret` (chua active)
  - Goi lai endpoint nay se tao secret moi (overwrite pending cu)

### Step 2: Xac nhan TOTP Setup

- **Method:** `POST /api/me/two-factor/confirm`
- **Auth:** Required — Permission `ManageTwoFactor`
- **Request:**
  ```json
  {
    "code": "123456"
  }
  ```
- **Response:** `200 OK`
  ```json
  {
    "recoveryCodes": [
      "a1b2c3d4e5",
      "f6g7h8i9j0",
      "k1l2m3n4o5",
      "p6q7r8s9t0",
      "u1v2w3x4y5",
      "z6a7b8c9d0",
      "e1f2g3h4i5",
      "j6k7l8m9n0"
    ]
  }
  ```
- **Ghi chu:**
  - User nhap ma 6 so tu Authenticator app
  - Server verify ma voi `PendingTwoFactorSecret`
  - Thanh cong: move pending → active, set `TwoFactorEnabled = true`, `TwoFactorProvider = totp`
  - Sinh 8 recovery codes (10 ky tu hex), hash bang `ITokenHasher`, luu vao DB
  - **Recovery codes chi hien thi 1 lan** — khong the xem lai
  - Neu user da co recovery codes cu, chung bi xoa va thay the

### Loi co the xay ra (Setup)
- `403 Forbidden` — Email chua confirmed
- `401 Unauthorized` — Ma TOTP khong hop le
- `400 Bad Request` — Chua goi setup truoc khi confirm

---

## Flow 2: Dang nhap voi 2FA

### Step 1: Dang nhap (Password)

- **Method:** `POST /api/auth/login`
- **Auth:** Anonymous
- **Request:** `{ "account": "...", "password": "...", "deviceId": "guid" }`
- **Response khi 2FA bat:** `200 OK`
  ```json
  {
    "accessToken": "eyJhbG...",
    "refreshToken": "",
    "accessTokenExpiresAt": "2026-03-19T10:03:00Z",
    "refreshTokenExpiresAt": "0001-01-01T00:00:00",
    "session": null,
    "requiresTwoFactor": true
  }
  ```
- **Ghi chu:**
  - `accessToken` la **limited JWT** (3 phut), chi chua claim `sub` + `purpose=2fa_verification`
  - `refreshToken` rong, `session` null — chua tao session
  - Frontend phai chuyen sang man hinh nhap ma TOTP

### Step 2: Xac thuc TOTP

- **Method:** `POST /api/auth/two-factor/verify`
- **Auth:** Required (dung limited 2FA JWT tu step 1)
- **Request:**
  ```json
  {
    "code": "123456",
    "deviceId": "guid"
  }
  ```
- **Response:** `200 OK` — `AuthTokenDto` day du (giong login binh thuong)
  ```json
  {
    "accessToken": "eyJhbG... (full JWT)",
    "refreshToken": "base64...",
    "accessTokenExpiresAt": "...",
    "refreshTokenExpiresAt": "...",
    "session": { ... },
    "requiresTwoFactor": false
  }
  ```
- **Ghi chu:**
  - Verify TOTP code truoc, neu sai → thu recovery code
  - **Replay prevention:** `LastUsedTotpTimeStep` ngan dung lai ma cu
  - Recovery code: single-use, hash verify, mark `IsUsed = true` sau khi dung
  - Thanh cong: tao session + refresh token + full JWT (giong login binh thuong)

### Loi co the xay ra (Login 2FA)
- `401 Unauthorized` — Ma TOTP sai VA khong match recovery code nao
- `401 Unauthorized` — Limited JWT het han (3 phut)
- `403 Forbidden` — 2FA chua bat cho user nay

---

## Flow 3: Tat 2FA

### Step 1: Disable

- **Method:** `POST /api/me/two-factor/disable`
- **Auth:** Required — Permission `ManageTwoFactor`
- **Request:** Khong co body
- **Response:** `204 No Content`
- **Ghi chu:**
  - Xoa `TwoFactorSecret`, `PendingTwoFactorSecret`, `LastUsedTotpTimeStep`
  - Set `TwoFactorEnabled = false`, `TwoFactorProvider = none`
  - Recovery codes **khong tu dong xoa** (co the them buoc nay)
  - Lan dang nhap tiep theo se khong yeu cau 2FA

---

## Flow 4: Tao lai Recovery Codes

### Step 1: Regenerate

- **Method:** `POST /api/me/two-factor/recovery-codes`
- **Auth:** Required — Permission `ManageTwoFactor`
- **Request:**
  ```json
  {
    "code": "123456"
  }
  ```
- **Response:** `200 OK`
  ```json
  {
    "recoveryCodes": ["a1b2c3d4e5", ...]
  }
  ```
- **Ghi chu:**
  - Phai verify TOTP code hien tai truoc khi regenerate
  - Codes cu bi xoa, 8 codes moi duoc tao
  - **Chi hien thi 1 lan**

---

## Technical Details

### TOTP Parameters
| Parameter | Value |
|-----------|-------|
| Algorithm | SHA-1 (RFC 6238) |
| Digits | 6 |
| Period | 30 seconds |
| Secret length | 20 bytes (160 bits) |
| Encoding | Base32 |
| Verification window | RFC specified network delay (±1 time step) |

### QR Code Format
```
otpauth://totp/{issuer}:{email}?secret={base32secret}&issuer={issuer}&digits=6&period=30
```

### Recovery Codes
- 8 codes, 10 ky tu hex moi code
- Hash bang `ITokenHasher` truoc khi luu
- Single-use — danh dau `IsUsed` sau khi verify thanh cong
- Luu trong table `recovery_codes`

### Database Schema

**Users table (columns moi):**
| Column | Type | Mo ta |
|--------|------|-------|
| `two_factor_secret` | varchar(255), nullable | Base32 TOTP secret (active) |
| `pending_two_factor_secret` | varchar(255), nullable | Secret tam thoi khi setup |
| `last_used_totp_time_step` | bigint, nullable | Chong replay attack |

**Recovery_codes table (moi):**
| Column | Type | Mo ta |
|--------|------|-------|
| `id` | uuid PK | RecoveryCodeId |
| `user_id` | uuid FK | Lien ket User |
| `code_hash` | varchar(255) | Hash cua recovery code |
| `is_used` | bool | Da su dung chua |
| `created_at` | timestamp | Thoi gian tao |
| `used_at` | timestamp, nullable | Thoi gian su dung |

### Packages su dung
- `Otp.NET` — TOTP generation & verification
- `QRCoder` — QR code PNG generation

### Security

- TOTP secret **nen duoc encrypt at rest** (hien luu plain Base32 — can cai thien)
- Limited JWT chi co 3 phut lifetime va claim `purpose=2fa_verification`
- Recovery codes hash truoc khi luu, khong the reverse
- Replay prevention qua `LastUsedTotpTimeStep`

## Domain Events & Side Effects

| Event | Mo ta |
|-------|-------|
| (chua co event rieng cho 2FA) | Co the them `TwoFactorEnabledEvent`, `TwoFactorDisabledEvent` trong tuong lai |

## Luu y nghiep vu

- User phai confirm email truoc khi co the setup TOTP
- Setup la 2 buoc: tao secret → confirm code. Khong duoc bat 2FA truoc khi user chung minh co the tao ma
- Khi 2FA bat, moi lan login se can 2 buoc: password → TOTP code
- Recovery codes la "cuu canh" khi mat dien thoai — user nen luu o noi an toan
- Tat 2FA khong yeu cau verify TOTP (chi can dang nhap day du) — co the siet lai
