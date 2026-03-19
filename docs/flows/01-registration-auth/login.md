# Dang nhap (Login)

## Tong quan
Nguoi dung dang nhap bang email hoac username va password. He thong tra ve access token (JWT), refresh token, va thong tin session.

## Actors
- **Nguoi dung da dang ky** (Anonymous)

## Endpoint Sequence

### Step 1: Dang nhap

- **Method:** `POST /api/auth/login`
- **Auth:** Anonymous (`AllowAnonymous`)
- **Request:**
  ```json
  {
    "account": "string (required - email hoac username)",
    "password": "string (required)",
    "deviceId": "guid (required - dinh danh thiet bi)"
  }
  ```
- **Response (2FA tat):** `200 OK`
  ```json
  {
    "accessToken": "string (JWT)",
    "refreshToken": "string",
    "accessTokenExpiresAt": "datetime",
    "refreshTokenExpiresAt": "datetime",
    "session": { ... },
    "requiresTwoFactor": false
  }
  ```
- **Response (2FA bat — TOTP):** `200 OK`
  ```json
  {
    "accessToken": "string (limited 2FA JWT, 3 phut)",
    "refreshToken": "",
    "accessTokenExpiresAt": "datetime (+ 3 phut)",
    "refreshTokenExpiresAt": "0001-01-01T00:00:00",
    "session": null,
    "requiresTwoFactor": true
  }
  ```
  → Frontend kiem tra `requiresTwoFactor`, neu `true` → chuyen sang man hinh nhap TOTP code.
  → Dung `accessToken` (limited JWT) de goi `POST /api/auth/two-factor/verify`.
  → Xem chi tiet tai [two-factor-auth.md](./two-factor-auth.md).
- **Loi co the xay ra:**
  - `401 Unauthorized` - Sai tai khoan hoac mat khau
  - `403 Forbidden` - Tai khoan bi khoa (Locked) hoac bi vo hieu hoa (Inactive)
  - `422 Unprocessable Entity` - Validation loi (account rong, v.v.)
  - `429 Too Many Requests` - Dang nhap sai qua nhieu lan (lockout)

## Business Logic (Handler)

1. **Normalize account:**
   - Neu chua `@` -> Xu ly nhu email (`UserEmail.Create`)
   - Nguoc lai -> Xu ly nhu username (`UserName.Create`)

2. **Tim user:** Tra cuu theo `Email.Normalized` hoac `UserName.Normalized`, include `Sessions`, `Tokens`, `Roles`, `LoginHistories`

3. **Kiem tra lockout:** `user.EnsureNotLockedOut(nowUtc)` - Kiem tra tai khoan co bi khoa tam thoi do sai mat khau nhieu lan

4. **Kiem tra trang thai:**
   - `UserStatus.Locked` -> Tra ve `UserLocked`
   - `UserStatus.Inactive` -> Tra ve `UserInactive`

5. **Xac thuc mat khau:** `user.Password.Verify(password, passwordHasher)`
   - Sai -> Ghi nhan `RecordFailedLogin`, co the tang lockout counter
   - Dung -> Tiep tuc

6. **Kiem tra 2FA (TOTP):**
   - Neu `user.TwoFactorEnabled && user.TwoFactorProvider == Totp`:
     - Ghi nhan dang nhap thanh cong (password OK)
     - Sinh limited 2FA JWT (3 phut, claim `purpose=2fa_verification`)
     - Tra ve `AuthTokenDto` voi `requiresTwoFactor = true`, session = null
     - **DUNG LAI** — khong tao session/refresh token
   - Neu 2FA tat -> Tiep tuc flow binh thuong

7. **Ghi nhan dang nhap thanh cong:** `user.RecordSuccessfulLogin(ipAddress, userAgent, nowUtc)`

7. **Tao session:** `user.CreateSession(deviceId, userAgent, ipAddress, slidingExpiration, absoluteExpiration, nowUtc)`

8. **Tao refresh token:**
   - Sinh random token (`_tokenProvider.Generate()`)
   - Hash token (`_tokenHasher.Hash(rawRefreshToken)`)
   - `user.CreateRefreshToken(sessionId, tokenHash, ipAddress, expiration, nowUtc)`

9. **Tao access token (JWT):**
   - Claims: `userId`, `email`, `userName`, `deviceId`, `roles`
   - `_tokenProvider.GenerateJwt(...)`

10. **Xoa revocation cache:** `ClearDeviceRevocationAsync`, `ClearUserRevocationAsync`

11. **Tra ve AuthTokenDto**

## Domain Events & Side Effects

| Event | Mo ta |
|-------|-------|
| `LoginAttemptedEvent` | Ghi nhan moi lan dang nhap (thanh cong hoac that bai) |
| `UserLockedOutEvent` | Khi tai khoan bi khoa do sai mat khau qua nhieu lan |

## Luu y nghiep vu

- **Account** co the la email hoac username - he thong tu dong phan biet bang ky tu `@`
- **DeviceId** la dinh danh thiet bi do client sinh ra, dung de quan ly multi-device session
- **IpAddress** va **UserAgent** duoc lay tu HttpContext de ghi nhan lich su
- Access token co thoi han ngan (thuong 15-30 phut)
- Refresh token co thoi han dai hon (thuong 7-30 ngay)
- Session co 2 loai expiration:
  - **Sliding:** Reset moi lan refresh token
  - **Absolute:** Khong reset, buoc phai dang nhap lai
- Khi dang nhap that bai, lockout counter tang len. Sau N lan sai -> khoa tam thoi
- He thong su dung **ISessionRevocationStore** (Redis cache) de revoke token nhanh
