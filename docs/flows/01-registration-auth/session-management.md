# Quan ly Phien Dang nhap (Session Management)

## Tong quan
Nguoi dung co the xem danh sach phien dang nhap dang hoat dong, lich su dang nhap va dang xuat (thu hoi phien).

## Actors
- **Nguoi dung da dang nhap**

## Endpoint Sequence

### Step 1: Xem cac phien dang hoat dong

- **Method:** `GET /api/me/sessions`
- **Auth:** Required - Permission `Me.ReadSessions`
- **Query Parameters:**
  ```
  ?deviceId=guid       (optional - loc theo thiet bi)
  &page=1              (phan trang)
  &pageSize=10
  ```
- **Response:** `200 OK`
  ```json
  {
    "items": [
      {
        "sessionId": "guid",
        "deviceId": "guid",
        "userAgent": "string",
        "ipAddress": "string",
        "createdAt": "datetime",
        "expiresAt": "datetime (sliding)",
        "absoluteExpiresAt": "datetime",
        "isActive": "boolean"
      }
    ],
    "totalCount": 10,
    "page": 1,
    "pageSize": 10
  }
  ```

### Step 2: Xem lich su dang nhap

- **Method:** `GET /api/me/login-history`
- **Auth:** Required - Permission `Me.ReadLoginHistory`
- **Query Parameters:**
  ```
  ?page=1
  &pageSize=10
  ```
- **Response:** `200 OK`
  ```json
  {
    "items": [
      {
        "ipAddress": "string",
        "userAgent": "string",
        "isSuccess": "boolean",
        "failureReason": "string?",
        "occurredAt": "datetime"
      }
    ],
    "totalCount": 50,
    "page": 1,
    "pageSize": 10
  }
  ```

### Step 3: Dang xuat

- **Method:** `POST /api/auth/logout`
- **Auth:** Required - Policy `ExpiredTokenAllowed`
- **Request:**
  ```json
  {
    "deviceId": "guid? (optional - neu co, chi dang xuat thiet bi cu the; neu null, dang xuat tat ca)"
  }
  ```
- **Response:** `204 No Content`

### Step 4: Doi mat khau (ket thuc session)

- **Method:** `PUT /api/me/password`
- **Auth:** Required - Permission `Me.ChangePassword`
- **Request:**
  ```json
  {
    "currentPassword": "string (required)",
    "newPassword": "string (required)"
  }
  ```
- **Response:** `204 No Content`
- **Loi co the xay ra:**
  - `401 Unauthorized` - Mat khau hien tai sai
  - `422 Unprocessable Entity` - Mat khau moi khong dat yeu cau

## Business Logic

### Logout Handler

1. **Lay user hien tai** tu `ICurrentUser`
2. **Neu co DeviceId:** Thu hoi session cua device do (`RevokeSession`)
3. **Neu khong co DeviceId:** Thu hoi tat ca session (`RevokeAllSession`)
4. **Cap nhat Redis cache:** Danh dau revocation trong `ISessionRevocationStore`
5. **Luu:** `SaveChangesAsync`

### Change Password Handler

1. **Lay user hien tai**, include `Password`
2. **Xac thuc mat khau cu:** `user.Password.Verify(currentPassword, passwordHasher)`
3. **Hash mat khau moi:** `Password.Create(newPassword, passwordHasher)`
4. **Cap nhat:** `user.ChangePassword(newPassword, nowUtc)`
5. **Luu:** `SaveChangesAsync`

## Background Jobs

### ExpiredSessionCleanupJob (moi 6 gio)

1. **Thu hoi session het absolute expiration:** `AbsoluteExpiresAt <= nowUtc`
2. **Thu hoi session het sliding expiration:** `ExpiresAt <= nowUtc`
3. **Thu hoi token mo coi:** Token thuoc session da bi thu hoi nhung chua bi danh dau revoked
4. **Xoa session cu:** Session khong hoat dong va cu hon 90 ngay

## Domain Events & Side Effects

| Event | Mo ta |
|-------|-------|
| `SessionRevokedEvent` | Khi phien bi thu hoi (logout) |
| `UserPasswordChangedEvent` | Khi mat khau duoc thay doi |

## Luu y nghiep vu

- **Logout** ho tro 2 che do: dang xuat 1 thiet bi (truyen `deviceId`) hoac dang xuat tat ca (khong truyen)
- **Policy `ExpiredTokenAllowed`**: Logout van hoat dong khi access token het han (can thiet cho UX)
- **Change password** khong tu dong dang xuat cac phien khac. Client can goi logout rieng neu muon
- **Login history** ghi nhan ca dang nhap thanh cong va that bai, bao gom IP va User-Agent
- **Session cleanup** chay tu dong, khong can admin can thiep
- He thong su dung Redis (`ISessionRevocationStore`) de revoke token tuc thi, khong phai doi den khi cleanup job chay
