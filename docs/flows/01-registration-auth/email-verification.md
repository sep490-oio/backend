# Xac thuc Email (Email Verification)

## Tong quan
Sau khi dang ky, nguoi dung can xac thuc email de kich hoat day du tai khoan. He thong su dung `ISecureTokenStore` de tao va validate token xac thuc.

## Actors
- **Nguoi dung da dang ky** (co the chua dang nhap)

## Endpoint Sequence

### Step 1: Xac thuc email

- **Method:** `POST /api/auth/confirm-email`
- **Auth:** Anonymous (`AllowAnonymous`)
- **Request:**
  ```json
  {
    "userId": "guid (required)",
    "token": "string (required)"
  }
  ```
- **Response:** `204 No Content`
- **Loi co the xay ra:**
  - `422 Unprocessable Entity` - Token khong hop le
  - `404 Not Found` - User khong ton tai
  - `409 Conflict` - Email da duoc xac thuc truoc do

### Step 2: Gui lai email xac thuc (neu can)

- **Method:** `POST /api/auth/resend-confirm-email`
- **Auth:** Anonymous (`AllowAnonymous`)
- **Request:**
  ```json
  {
    "email": "string (required)"
  }
  ```
- **Response:** `204 No Content`
- **Loi co the xay ra:**
  - `422 Unprocessable Entity` - Email khong hop le

## Business Logic (Confirm Email Handler)

1. **Tim user:** Tra cuu user theo `UserId`
2. **Validate token:** Su dung `ISecureTokenStore.ValidateTokenAsync(TokenType.EmailVerification, userId, token)`
3. **Xac thuc:** Goi `user.ConfirmEmail(nowUtc)` - Cap nhat `EmailConfirmedAt`
4. **Huy token:** Goi `ISecureTokenStore.InvalidateTokenAsync(TokenType.EmailVerification, userId)` - Dam bao token chi dung 1 lan
5. **Luu thay doi:** `SaveChangesAsync`

## Business Logic (Resend Confirm Email Handler)

1. **Tim user theo email** (normalize truoc)
2. **Tao token moi** qua `ISecureTokenStore`
3. **Gui email** chua link xac thuc

## Domain Events & Side Effects

| Event | Mo ta |
|-------|-------|
| `UserEmailConfirmedEvent` | Phat ra khi email duoc xac thuc thanh cong |

## Luu y nghiep vu

- Token xac thuc email la one-time use, sau khi dung se bi invalidate
- Neu user da xac thuc email roi ma goi lai se nhan loi `Conflict`
- Link xac thuc thong thuong co dang: `{frontend_url}/confirm-email?userId={userId}&token={token}`
- Resend confirm email luon tra ve `204` bat ke email co ton tai hay khong (bao mat, chong enum)
