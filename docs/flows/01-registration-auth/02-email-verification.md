# 02 - Email Verification (Xac thuc email)

Module nay gom 2 endpoints: xac nhan email va gui lai email xac thuc.

---

## Endpoint 1: Confirm Email

| Thuoc tinh | Gia tri |
|-----------|---------|
| Method | `POST` |
| Route | `/api/auth/confirm-email` |
| Auth | Anonymous (`.AllowAnonymous()`) |
| Success Response | `204 No Content` |
| Command | `ConfirmEmailCommand` |
| Handler | `ConfirmEmailCommandHandler` |

### Request Schema

```json
{
  "userId": "guid",
  "token": "string"
}
```

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `UserId` | `Guid` | Yes | NotEmptyGuid |
| `Token` | `string` | Yes | NotWhiteSpace |

### Business Logic

1. **Parse UserId**: `UserId.From(request.UserId)`
2. **Load user**: `_dbContext.GetByIdAsync<User, UserId>(userId)` → neu null: `User.NotFound(userId)` (404)
3. **Validate token**: `_secureTokenStore.ValidateTokenAsync(TokenType.EmailVerification, userId, request.Token)`
   - Token duoc hash va so sanh voi hash luu trong store
   - Kiem tra TTL (token chua het han)
   - Neu invalid: `User.ConfirmationToken.Invalid` (401)
4. **Confirm email**: `user.ConfirmEmail(now)` thuc hien:
   - Kiem tra `EnsureNotDeleted()` → neu deleted: `User.Deleted` (403)
   - Neu `EmailConfirmed == true` → return success (idempotent)
   - Set `EmailConfirmed = true`, `EmailConfirmedAt = now`
   - **Auto-activate**: Neu `Status == Inactive` → `ChangeStatus(Active, now)`
     - Raise `UserStatusChangedEvent(oldStatus: "inactive", newStatus: "active")`
   - Neu `Status != Inactive` → Raise `UserEmailConfirmedEvent(userId, email, now)`
5. **Invalidate token**: `_secureTokenStore.InvalidateTokenAsync(TokenType.EmailVerification, userId)` — token chi dung 1 lan
6. **Persist**: `_unitOfWork.SaveChangesAsync()`

### Domain Events Triggered

| Event | Dieu kien | Handler |
|-------|-----------|---------|
| `UserStatusChangedEvent` | Khi user tu Inactive → Active | `UserStatusChangedEventHandler`: ghi AuditLog + gui email + tao notification |
| `UserEmailConfirmedEvent` | Khi user da Active truoc do | `UserEmailConfirmedEventHandler`: tao notification "Email da duoc xac thuc" + gui email confirmed |

---

## Endpoint 2: Resend Confirm Email

| Thuoc tinh | Gia tri |
|-----------|---------|
| Method | `POST` |
| Route | `/api/auth/resend-confirm-email` |
| Auth | Anonymous (`.AllowAnonymous()`) |
| Success Response | `204 No Content` |
| Command | `ResendConfirmEmailCommand` |
| Handler | `ResendConfirmEmailCommandHandler` |

### Request Schema

```json
{
  "email": "string"
}
```

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `Email` | `string` | Yes | NotWhiteSpace, Regex `^[^@\s]+@[^@\s]+\.[^@\s]+$` |

### Business Logic

1. **Create value object**: `UserEmail.Create(request.Email)` → normalize email
2. **Find user**: `FirstOrDefaultAsync(x => x.Email == email)`
   - **Neu user khong ton tai**: return `Success` (silent — khong tiet lo email co trong he thong hay khong)
3. **Check da xac thuc**: `user.EmailConfirmedAt is not null` → return `Success` (silent)
4. **Request verification**: `user.RequestEmailVerification(now)` thuc hien:
   - `EnsureNotDeleted()` + `EnsureNotLockedOut(now)`
   - Kiem tra `EmailConfirmedAt is not null` → return error `User.Email.NotConfirmed` (logic dao: neu da confirm thi khong can gui lai)
   - Raise `EmailVerificationRequestedEvent(userId, email, userName, now)`
5. **Persist**: `_unitOfWork.SaveChangesAsync()` → dispatch event

### Event Handler: EmailVerificationRequestedEventHandler

Handler `EmailVerificationRequestedEventHandler` xu ly voi cooldown mechanism:

1. **Check cooldown**: `_secureTokenStore.GetTokenTtlAsync(TokenType.EmailVerification, userId)`
   - Neu token con song: tinh `elapsed = totalExpiration - remainingTtl`
   - Neu `elapsed < ResendEmailCooldown` (default 60s): **skip** gui email, log warning
2. **Create new token**: `_secureTokenStore.CreateTokenAsync(TokenType.EmailVerification, userId, totalExpiration)`
   - `totalExpiration` = `_runtimeSettings.Auth.EmailVerificationTokenExpiration` (default 30 phut)
   - Replaces any existing token cua cung type+userId
3. **Send email**: `_mailNotifier.SendResendVerifyAsync(email, userName, token, userId, tokenExpiry)`

---

## ISecureTokenStore Mechanism

`ISecureTokenStore` la abstraction cho viec luu tru token bao mat (duoc implement trong infrastructure layer):

| Method | Mo ta |
|--------|-------|
| `CreateTokenAsync(type, userId, expiration?)` | Tao token moi, hash va luu. Thay the token cu cung type. Tra ve plain token + TTL |
| `ValidateTokenAsync(type, userId, plainToken)` | Hash plain token va so sanh voi stored hash. Kiem tra TTL. Tra ve `bool` |
| `InvalidateTokenAsync(type, userId)` | Xoa token (sau khi su dung thanh cong) |
| `HasActiveTokenAsync(type, userId)` | Kiem tra co token con hoat dong khong |
| `GetTokenTtlAsync(type, userId)` | Lay thoi gian con lai cua token (cho cooldown) |

**Dac diem**:
- **Hash-based**: Token duoc hash truoc khi luu (chi luu hash, khong luu plain text)
- **TTL**: Moi token co thoi gian song (configurable)
- **One-time use**: Token bi invalidate sau khi dung
- **Single token per type+user**: Tao token moi se thay the token cu

**TokenType enum values**: `EmailVerification`, `PasswordReset`, `PhoneVerification`, `TwoFactorSetup`, `AccountDeletion`

---

## Cooldown Configuration

| Config | Default | Mo ta |
|--------|---------|-------|
| `Auth.EmailVerificationTokenExpiration` | 30 phut | Thoi gian song cua email verification token |
| `Auth.ResendEmailCooldown` | 60 giay | Thoi gian toi thieu giua 2 lan gui email |
| `Auth.PasswordResetTokenExpiration` | 30 phut | Thoi gian song cua password reset token |

Cooldown duoc tinh bang: `elapsed = totalExpiration - remainingTtl`. Neu `elapsed < cooldown` → khong gui email, chi log warning.

---

## Error Codes

### Confirm Email

| HTTP Status | Error Code | Mo ta | Khi nao |
|------------|------------|-------|---------|
| 422 | `ViolationsError` | Validation that bai | UserId empty hoac Token empty |
| 404 | `User.NotFound` | User khong ton tai | userId khong tim thay trong DB |
| 401 | `User.ConfirmationToken.Invalid` | Token khong hop le hoac het han | `ValidateTokenAsync` tra ve false |
| 403 | `User.Deleted` | User da bi xoa | `IsDeleted == true` |

### Resend Confirm Email

| HTTP Status | Error Code | Mo ta | Khi nao |
|------------|------------|-------|---------|
| 422 | `ViolationsError` | Validation that bai | Email format khong hop le |
| 204 | _(success)_ | Luon tra ve success | Ke ca khi user khong ton tai hoac email da xac thuc (silent success pattern) |

> **Luu y**: Resend endpoint su dung **silent success pattern** — luon tra ve 204 bat ke user co ton tai hay khong, de tranh tiet lo thong tin email co trong he thong.

---

## Source Files

| Layer | File |
|-------|------|
| Command (Confirm) | `src/core/OIO.Application/Context/UserContext/Commands/ConfirmEmail/ConfirmEmailCommand.cs` |
| Handler (Confirm) | `src/core/OIO.Application/Context/UserContext/Commands/ConfirmEmail/ConfirmEmailCommandHandler.cs` |
| Command + Handler (Resend) | `src/core/OIO.Application/Context/UserContext/Commands/ResendConfirmEmail/ResendConfirmEmailCommand.cs` |
| Domain | `src/core/OIO.Domain/Context/UserContext/Aggregates/Users/User.cs` (methods `ConfirmEmail`, `RequestEmailVerification`) |
| Event Handler (Resend) | `src/core/OIO.Application/Context/UserContext/EventHandlers/EmailVerificationRequestedEventHandler.cs` |
| Event Handler (Confirmed) | `src/core/OIO.Application/Context/UserContext/EventHandlers/UserEmailConfirmedEventHandler.cs` |
| ISecureTokenStore | `src/core/OIO.Application/Abstractions/Security/ISecureTokenStore.cs` |
| Endpoint (Confirm) | `src/presentation/OIO.Api/Endpoints/UserContext/Auth/ConfirmEmailEndpoint.cs` |
| Endpoint (Resend) | `src/presentation/OIO.Api/Endpoints/UserContext/Auth/ResendConfirmEmailEndpoint.cs` |
