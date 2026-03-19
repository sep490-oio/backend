# Lam moi Token (Refresh Token)

## Tong quan
Khi access token het han, client su dung refresh token de lay cap token moi ma khong can dang nhap lai. He thong ap dung **token rotation** - moi lan refresh, refresh token cu bi thu hoi va token moi duoc cap.

## Actors
- **Nguoi dung da dang nhap** (co the access token da het han)

## Endpoint Sequence

### Step 1: Refresh token

- **Method:** `POST /api/auth/refresh`
- **Auth:** Required - Policy `ExpiredTokenAllowed` (cho phep access token da het han)
- **Request:**
  ```json
  {
    "refreshToken": "string (required)",
    "deviceId": "guid (required)"
  }
  ```
- **Response:** `200 OK`
  ```json
  {
    "accessToken": "string (JWT moi)",
    "refreshToken": "string (refresh token moi)",
    "accessTokenExpiresAt": "datetime",
    "refreshTokenExpiresAt": "datetime",
    "session": {
      "sessionId": "guid",
      "deviceId": "guid",
      "slidingExpiresAt": "datetime",
      "absoluteExpiresAt": "datetime",
      "isNearingAbsoluteExpiration": "boolean",
      "remainingAbsoluteTime": "timespan"
    }
  }
  ```
- **Loi co the xay ra:**
  - `401 Unauthorized` - Refresh token khong hop le hoac da bi thu hoi
  - `403 Forbidden` - Device mismatch (nghi ngo danh cap token)

## Business Logic (Handler)

1. **Lay user hien tai** tu `ICurrentUser.UserId` (tu expired JWT), include Sessions + Tokens + Roles

2. **Kiem tra DeviceId:**
   - Neu `request.DeviceId != currentUser.DeviceId` (tu access token):
     - **Thu hoi TOAN BO session** cua user (`RevokeAllSession`)
     - Revoke trong Redis cache (`RevokeAllDevicesAsync`)
     - Tra ve loi `RefreshToken.Revoked`
   - Day la co che **chong danh cap token** - neu ke tan cong dung token tren thiet bi khac, tat ca session bi thu hoi

3. **Tim refresh token:** Hash token va so sanh voi tat ca token trong cac session cua user

4. **Kiem tra session-device match:**
   - Neu `session.DeviceId != request.DeviceId`:
     - Thu hoi rieng session do (`RevokeSession`)
     - Tra ve `DeviceMismatch`

5. **Token Rotation:**
   - Sinh refresh token moi (`_tokenProvider.Generate()`)
   - Hash token moi
   - `user.RotateRefreshToken(sessionId, currentToken, newTokenHash, ipAddress, expirations, nowUtc)`
   - Token cu bi danh dau `Revoked`, token moi duoc tao

6. **Tao access token moi:** JWT voi claims cap nhat (roles co the da thay doi)

7. **Tra ve AuthTokenDto moi**

## Bao mat - Token Rotation

```
[Refresh #1] --> [Refresh #2] --> [Refresh #3]
    (revoked)       (revoked)       (active)

Neu ke tan cong dung [Refresh #1] da revoked:
  -> He thong phat hien REUSE
  -> Thu hoi TOAN BO session
  -> Tat ca thiet bi bi dang xuat
```

## Domain Events & Side Effects

| Event | Mo ta |
|-------|-------|
| `RefreshTokenRotatedEvent` | Ghi nhan moi lan rotate token |
| `SessionRevokedEvent` | Khi session bi thu hoi do device mismatch |
| `SessionNearingExpirationEvent` | Khi session sap het absolute expiration |

## Luu y nghiep vu

- **Policy `ExpiredTokenAllowed`**: Cho phep gui request voi access token da het han (can thiet de refresh)
- **Token Rotation**: Moi refresh token chi dung duoc 1 lan. Sau khi dung, no bi revoke va token moi duoc cap
- **Device binding**: Refresh token gan voi DeviceId. Neu DeviceId thay doi -> thu hoi phien
- **Sliding expiration**: Moi lan refresh thanh cong, sliding expiration cua session duoc reset
- **Absolute expiration**: Khong reset, dam bao user phai dang nhap lai dinh ky
- **isNearingAbsoluteExpiration**: Client co the su dung flag nay de canh bao user dang nhap lai
