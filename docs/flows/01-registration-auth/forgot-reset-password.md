# Quen & Dat lai Mat khau (Forgot & Reset Password)

## Tong quan
Luong cho phep nguoi dung khoi phuc tai khoan khi quen mat khau. Gom 2 buoc: yeu cau reset (gui email) va dat mat khau moi (dung token tu email).

## Actors
- **Nguoi dung da dang ky** (chua dang nhap)

## Endpoint Sequence

### Step 1: Yeu cau dat lai mat khau

- **Method:** `POST /api/auth/forgot-password`
- **Auth:** Anonymous (`AllowAnonymous`)
- **Request:**
  ```json
  {
    "email": "string (required, email hop le)"
  }
  ```
- **Response:** `204 No Content`
- **Loi co the xay ra:**
  - `422 Unprocessable Entity` - Email khong hop le (format sai)

### Step 2: Dat lai mat khau

- **Method:** `POST /api/auth/reset-password`
- **Auth:** Anonymous (`AllowAnonymous`)
- **Request:**
  ```json
  {
    "email": "string (required)",
    "token": "string (required - token nhan tu email)",
    "newPassword": "string (required, 8-128 ky tu, theo quy tac)",
    "confirmPassword": "string (required, phai giong newPassword)"
  }
  ```
- **Response:** `204 No Content`
- **Loi co the xay ra:**
  - `401 Unauthorized` - Token khong hop le hoac het han
  - `422 Unprocessable Entity` - Mat khau khong dat yeu cau, confirm khong khop

## Business Logic

### Forgot Password Handler

1. **Validate email:** `UserEmail.Create(request.Email)`
2. **Tim user:** Tra cuu theo email
3. **Bao mat:** Neu user khong ton tai, **van tra ve thanh cong** (chong enum account)
4. **Kiem tra:** Chi xu ly neu email da duoc xac thuc (`EmailConfirmedAt is not null`)
5. **Tao reset request:** `user.RequestPasswordReset(nowUtc)` - Raise domain event, tao token qua event handler
6. **Gui email:** Event handler gui email chua link reset

### Reset Password Handler

1. **Tim user theo email**
2. **Validate token:** `ISecureTokenStore.ValidateTokenAsync(TokenType.PasswordReset, userId, token)`
3. **Hash mat khau moi:** `_passwordHasher.Hash(request.NewPassword)`
4. **Cap nhat:** `user.ChangePassword(password, nowUtc)`
5. **Huy token:** `ISecureTokenStore.InvalidateTokenAsync(TokenType.PasswordReset, userId)`
6. **Luu:** `SaveChangesAsync`

## Validation Rules

| Field | Rule |
|-------|------|
| `email` | Khong rong, dung dinh dang email |
| `token` | Khong rong |
| `newPassword` | 8-128 ky tu, theo `App.Constraint.Password.Validator` (hoa, thuong, so, ky tu dac biet) |
| `confirmPassword` | Phai giong `newPassword` |

## Domain Events & Side Effects

| Event | Mo ta |
|-------|-------|
| `UserPasswordChangedEvent` | Phat ra khi mat khau duoc thay doi thanh cong |

## Luu y nghiep vu

- **Forgot password luon tra ve 204** bat ke email co ton tai hay khong -> chong enum tai khoan
- **Chi gui email neu email da xac thuc** (`EmailConfirmedAt is not null`) -> tranh gui cho email chua verify
- Token reset password la **one-time use**, sau khi dung se bi invalidate
- Token co thoi han (thuong 24h), qua thoi han se khong su dung duoc
- Mat khau moi phai tuan theo tat ca quy tac: do dai toi thieu, ky tu hoa, ky tu thuong, so, ky tu dac biet
- `confirmPassword` duoc validate o tang command (server-side)
