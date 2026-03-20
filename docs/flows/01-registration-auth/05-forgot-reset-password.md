# 05 - Forgot & Reset Password

## Endpoints

| Method | Path | Auth | Response |
|--------|------|------|----------|
| `POST` | `/api/auth/forgot-password` | None | `204 No Content` |
| `POST` | `/api/auth/reset-password` | None | `204 No Content` |

---

## Forgot Password

### Request

```json
{
  "email": "string"
}
```

**Validation** (`IHasValidate`):
- `Email` - must not be whitespace, must match `App.Constraint.UserEmail.Regex`

### Flow

The endpoint **always returns 204 (success)** regardless of whether the email exists. This prevents email enumeration attacks.

```
Client                  Handler                     User Aggregate              EventHandler                    SecureTokenStore          MailNotifier
  |                       |                             |                           |                               |                        |
  |-- POST forgot-pwd --->|                             |                           |                               |                        |
  |                       |-- Validate email format --->|                           |                               |                        |
  |                       |-- Find user by email ------>|                           |                               |                        |
  |                       |                             |                           |                               |                        |
  |                       | (user not found? return 204 silently)                   |                               |                        |
  |                       |                             |                           |                               |                        |
  |                       | (email not confirmed? return 204 silently)              |                               |                        |
  |                       |                             |                           |                               |                        |
  |                       |-- RequestPasswordReset() -->|                           |                               |                        |
  |                       |                             |-- EnsureNotDeleted ------->|                               |                        |
  |                       |                             |-- EnsureNotLockedOut ----->|                               |                        |
  |                       |                             |-- Check EmailConfirmedAt ->|                               |                        |
  |                       |                             |-- Raise PasswordResetRequestedEvent                        |                        |
  |                       |                             |                           |                               |                        |
  |                       |-- SaveChanges ------------->|                           |                               |                        |
  |<-- 204 No Content ----|                             |                           |                               |                        |
  |                       |                             |                           |                               |                        |
  |                       |                             |                           |-- GetTokenTtlAsync ----------->|                        |
  |                       |                             |                           |    (check cooldown)            |                        |
  |                       |                             |                           |                               |                        |
  |                       |                             |                           | (if within cooldown, abort)    |                        |
  |                       |                             |                           |                               |                        |
  |                       |                             |                           |-- CreateTokenAsync ----------->|                        |
  |                       |                             |                           |    (type: PasswordReset)       |                        |
  |                       |                             |                           |                               |                        |
  |                       |                             |                           |-- SendPasswordResetAsync ----->|----------------------->|
  |                       |                             |                           |    (email, userName, token)    |                        |
```

### Handler Details (`ForgotPasswordCommandHandler`)

1. **Validate email format** via `UserEmail.Create(request.Email)`.
2. **Find user** by email. If not found, return success silently.
3. **Check email confirmed**: If `user.EmailConfirmedAt is not null`, return success (handler line 58). Note: the `RequestPasswordReset()` domain method independently checks `EmailConfirmedAt is null` and rejects unconfirmed emails.
4. Call `user.RequestPasswordReset(nowUtc)` which:
   - Calls `EnsureNotDeleted()` and `EnsureNotLockedOut(now)`.
   - Checks `EmailConfirmedAt is null` -- returns `User.Email.NotConfirmed` if unconfirmed.
   - Raises `PasswordResetRequestedEvent(UserId, Email, UserName, OccurredAt)`.
5. Save changes to dispatch the domain event.

### Event Handler (`PasswordResetRequestedEventHandler`)

The domain event is handled asynchronously:

1. **Cooldown check**: Calls `ISecureTokenStore.GetTokenTtlAsync(PasswordReset, userId)` to see if a token already exists.
   - Computes `elapsed = totalExpiration - ttl`.
   - If `elapsed < Auth.ResendEmailCooldown`, logs a warning and **aborts** (no new email sent).
2. **Create token**: Calls `ISecureTokenStore.CreateTokenAsync(PasswordReset, userId, totalExpiration)` which generates a secure token (hashed for storage, plain returned).
3. **Send email**: Calls `IUserMailNotifier.SendPasswordResetAsync(email, userName, token, tokenExpiry)`.

---

## Reset Password

### Request

```json
{
  "email": "string",
  "token": "string",
  "newPassword": "string",
  "confirmPassword": "string"
}
```

**Validation** (`IHasValidate`):
- `Email` - must not be whitespace, must match email regex
- `Token` - must not be whitespace
- `NewPassword` - must not be whitespace, `MinLength` to `MaxLength`, must pass `App.Constraint.Password.Validator` format rules
- `ConfirmPassword` - must not be whitespace, must equal `NewPassword`

### Flow

1. **Validate email format** via `UserEmail.Create(request.Email)`.
2. **Find user** by email. If not found, return `User.Credentials.Invalid`.
3. **Validate token** via `ISecureTokenStore.ValidateTokenAsync(PasswordReset, userId, token)`. If invalid/expired, return `User.ConfirmationToken.Invalid`.
4. **Hash new password** via `IPasswordHasher.Hash(request.NewPassword)`.
5. **Create Password value object** via `Password.CreateFromHash(hashedPassword)`.
6. **Change password** via `user.ChangePassword(password, nowUtc)` which:
   - Calls `EnsureNotDeleted()` and `EnsureNotLockedOut(now)`.
   - Sets `Password = newPassword` and `ModifiedAt = nowUtc`.
   - Raises `UserPasswordChangedEvent(UserId, nowUtc)`.
7. **Invalidate token** via `ISecureTokenStore.InvalidateTokenAsync(PasswordReset, userId)` -- token is single-use.
8. **Save changes**.

### Important: Reset Password Does NOT Revoke Sessions

Unlike a deliberate password change flow that might revoke active sessions, `ResetPassword` only calls `user.ChangePassword()` which:
- Updates the password hash
- Raises `UserPasswordChangedEvent`
- Does **NOT** call `RevokeAllSession()` or interact with `ISessionRevocationStore`

This means existing sessions and refresh tokens remain valid after a password reset. If a user's account was compromised and they reset their password, the attacker's existing sessions would still be active until they expire or are manually revoked.

---

## Security Measures

| Measure | Implementation |
|---------|---------------|
| **No email enumeration** | `forgot-password` always returns 204, even if the email does not exist or is unconfirmed |
| **Cooldown enforcement** | `PasswordResetRequestedEventHandler` checks token TTL; if less than `ResendEmailCooldown` has elapsed since last token, the request is silently dropped |
| **Token expiration** | Reset tokens have a configurable TTL (`Auth.PasswordResetTokenExpiration` from `IRuntimeSettings`) |
| **Single-use tokens** | After successful reset, `InvalidateTokenAsync` is called to delete the token |
| **Token hashing** | `ISecureTokenStore` stores hashed tokens, not plaintext |
| **Password validation** | New password must meet minimum length, maximum length, and format rules (`App.Constraint.Password`) |
| **Password confirmation** | `ConfirmPassword` must exactly match `NewPassword` |
| **Account state checks** | Domain method checks `EnsureNotDeleted()` and `EnsureNotLockedOut()` before allowing password change |

---

## Error Codes

### Forgot Password

The endpoint always returns `204 No Content`. No error codes are returned to the caller. Failures are handled silently:

| Scenario | Behavior |
|----------|----------|
| Email format invalid | Returns validation error (from `IHasValidate`) |
| User not found | Returns 204 (silent success) |
| Email not confirmed | Returns 204 (silent success) |
| User deleted / locked | Error from `RequestPasswordReset()` is returned, but domain event is not raised |
| Cooldown active | Event handler silently drops the request (no email sent) |

### Reset Password

| Code | HTTP | When |
|------|------|------|
| `User.Credentials.Invalid` | 401 | User not found by email |
| `User.ConfirmationToken.Invalid` | 401 | Token is invalid, expired, or already used |
| `User.Deleted` | 403 | User has been soft-deleted |
| `User.Locked` | 403 | User account is locked out |

---

## Source Files

- `src/core/OIO.Application/Context/UserContext/Commands/ForgotPassword/ForgotPasswordCommand.cs` (command + handler)
- `src/core/OIO.Application/Context/UserContext/Commands/ResetPassword/ResetPasswordCommand.cs` (command + handler)
- `src/core/OIO.Application/Context/UserContext/EventHandlers/PasswordResetRequestedEventHandler.cs`
- `src/core/OIO.Domain/Context/UserContext/Aggregates/Users/User.cs` (RequestPasswordReset, ChangePassword)
- `src/core/OIO.Application/Abstractions/Security/ISecureTokenStore.cs`
- `src/core/OIO.Domain/Context/UserContext/Errors/UserErrors.cs`
