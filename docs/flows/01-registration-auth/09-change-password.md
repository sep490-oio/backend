# 09 - Change Password

## Overview

Authenticated users can change their password via `PUT /api/me/password`. This flow verifies the current password, hashes the new one, revokes **all** active sessions, and blacklists all outstanding JWTs to force re-authentication on every device.

---

## Endpoint

| Property | Value |
|----------|-------|
| Method | `PUT` |
| Path | `/api/me/password` |
| Auth | Required (Bearer JWT) |
| Command | `ChangePasswordCommand` |
| Handler | `ChangePasswordCommandHandler` |

### Request

```json
{
  "CurrentPassword": "string",
  "NewPassword": "string"
}
```

### Validation (IHasValidate)

| Field | Rule |
|-------|------|
| `CurrentPassword` | Not null or whitespace |
| `NewPassword` | Not null or whitespace, min length (`App.Constraint.Password.MinLength`), max length (`App.Constraint.Password.MaxLength`), format regex (`App.Constraint.Password.Validator`) |

---

## Business Logic Flow

1. **Load user** — Fetch the authenticated user by `ICurrentUser.UserId`, eagerly including `Sessions` and their `Tokens`.
2. **Verify current password** — Call `user.Password.Verify(request.CurrentPassword, _passwordHasher)`. If verification fails, return `UserErrors.User.InvalidCredentials`.
3. **Hash new password** — `Password.Create(request.NewPassword, _passwordHasher)`. If creation fails (e.g., validation), return the error.
4. **Apply domain change** — `user.ChangePassword(newPassword, nowUtc)`:
   - Sets `Password = newPassword`
   - Sets `ModifiedAt = nowUtc`
   - Raises `UserPasswordChangedEvent(UserId, nowUtc)`
   - Internal guard: `EnsureNotDeleted()` then `EnsureNotLockedOut(nowUtc)` — returns error if either fails.
5. **Revoke ALL sessions** — `user.RevokeAllSession("Password changed", nowUtc)`:
   - Iterates all active sessions, calls `session.Revoke(reason, now)` on each.
   - Raises `SessionRevokedEvent` per session.
6. **Persist** — `SaveChangesAsync` flushes entity changes and domain events.
7. **Blacklist all JWTs** — `_revocationStore.RevokeAllDevicesAsync(userId)` sets cache key `"revoked:user:{userId}"` so the auth middleware rejects all existing access tokens for this user.

### Event: UserPasswordChangedEvent

When the `UserPasswordChangedEvent` is dispatched, `UserPasswordChangedEventHandler` performs:

1. Loads the user to get email and username.
2. Sends a **password changed alert email** via `IUserMailNotifier.SendPasswordChangedAlertAsync(email, userName)`.
3. Calls `ISessionRevocationStore.RevokeAllDevicesAsync(userId)` (cache-level blacklist as an additional safety net).

---

## Change Password vs Reset Password

| Aspect | Change Password | Reset Password |
|--------|----------------|----------------|
| Endpoint | `PUT /api/me/password` | `POST /api/auth/reset-password` |
| Authentication | Required (Bearer JWT) | Not required (uses token from email) |
| Requires current password | Yes (`Password.Verify`) | No (validated via `ISecureTokenStore` token) |
| Input fields | `CurrentPassword`, `NewPassword` | `Email`, `Token`, `NewPassword`, `ConfirmPassword` |
| Revokes all sessions | Yes (`user.RevokeAllSession`) | No |
| Blacklists all JWTs | Yes (`RevokeAllDevicesAsync`) | No |
| Invalidates reset token | N/A | Yes (`ISecureTokenStore.InvalidateTokenAsync`) |
| Domain event raised | `UserPasswordChangedEvent` | `UserPasswordChangedEvent` |
| Email alert sent | Yes (via event handler) | Yes (via event handler) |
| Confirm password field | No | Yes (`ConfirmPassword` must equal `NewPassword`) |

---

## Error Codes

| Error Code | HTTP Status | Description | When |
|------------|-------------|-------------|------|
| `User.NotFound` | 404 | User with the given ID was not found | Current user no longer exists |
| `User.Credentials.Invalid` | 401 | The provided email or password is incorrect | Current password verification fails |
| `User.Deleted` | 403 | User has been deleted | `EnsureNotDeleted()` guard in domain |
| `User.Locked` | 403 | User is locked due to suspension | `EnsureNotLockedOut()` guard in domain |

---

## Dependencies

| Interface | Usage |
|-----------|-------|
| `IDbContext` | Load user aggregate with sessions and tokens |
| `IUnitOfWork` | Persist changes and dispatch domain events |
| `IPasswordHasher` | Verify current password, hash new password |
| `ICurrentUser` | Resolve the authenticated user's ID |
| `ISessionRevocationStore` | Blacklist all access tokens at cache level |
| `IClock` | Provide `UtcNow` for timestamps |

---

## Related Source Files

| File | Path |
|------|------|
| ChangePasswordCommand | `src/core/OIO.Application/Context/UserContext/Commands/ChangePassword/ChangePasswordCommand.cs` |
| ChangePasswordCommandHandler | `src/core/OIO.Application/Context/UserContext/Commands/ChangePassword/ChangePasswordCommandHandler.cs` |
| ResetPasswordCommand | `src/core/OIO.Application/Context/UserContext/Commands/ResetPassword/ResetPasswordCommand.cs` |
| User.ChangePassword | `src/core/OIO.Domain/Context/UserContext/Aggregates/Users/User.cs` |
| UserPasswordChangedEvent | `src/core/OIO.Domain/Context/UserContext/Aggregates/Users/Events/UserPasswordChangedEvent.cs` |
| UserPasswordChangedEventHandler | `src/core/OIO.Application/Context/UserContext/EventHandlers/UserPasswordChangedEventHandler.cs` |
