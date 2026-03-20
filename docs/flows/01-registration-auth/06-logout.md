# 06 - Logout

## Endpoint

| Method | Path | Auth |
|--------|------|------|
| `POST` | `/api/auth/logout` | Requires valid access token (JWT) |

## Request

```json
{
  "deviceId": "guid (optional)"
}
```

**Validation** (`IHasValidate`):
- `DeviceId` -- when provided, must not be an empty GUID (`NotEmptyGuid`)

## Response

`204 No Content` on success.

## Two Logout Paths

The handler branches based on whether `DeviceId` is provided **and** matches the current user's device.

### Path 1: Specific Device Logout

**Condition**: `request.DeviceId.HasValue && request.DeviceId.Value == currentUser.DeviceId`

This logs out only the session associated with the specified device.

| Step | Action |
|------|--------|
| 1 | Load user with Sessions and Tokens |
| 2 | Call `user.RevokeSessionByDevice(deviceId, "User logout", nowUtc)` |
| 3 | Domain finds the first active session matching `deviceId` |
| 4 | Session is revoked: `IsActive = false`, `RevokedAt = now`, `RevokedReason = "User logout"` |
| 5 | All tokens in the session are revoked: `RevokedAt = now`, `RevokedReason = "User logout"` |
| 6 | `SessionRevokedEvent` is raised |
| 7 | `ISessionRevocationStore.RevokeDeviceAsync(userId, deviceId)` adds device to blacklist (cache key: `revoked:device:{userId}:{deviceId}`) |
| 8 | Save changes |

### Path 2: All Sessions Logout

**Condition**: `DeviceId` is `null`, or `DeviceId` does not match `currentUser.DeviceId`

This logs out all active sessions for the user.

| Step | Action |
|------|--------|
| 1 | Load user with Sessions and Tokens |
| 2 | Call `user.RevokeAllSession("User logout all", nowUtc)` |
| 3 | Domain iterates all active sessions and revokes each one |
| 4 | Each session: `IsActive = false`, `RevokedAt = now`, all tokens revoked |
| 5 | A `SessionRevokedEvent` is raised **per session** |
| 6 | `ISessionRevocationStore.RevokeAllDevicesAsync(userId)` adds user-level blacklist (cache key: `revoked:user:{userId}`) |
| 7 | Save changes |

> **Note**: If `DeviceId` is provided but does **not** match `currentUser.DeviceId`, the handler falls through to the `else` branch and revokes ALL sessions (not just the specified device). This is the actual code behavior based on the condition `request.DeviceId.HasValue && request.DeviceId.Value == _currentUser.DeviceId`.

## Revocation Details

### Session Revocation (Domain)

When `UserSession.Revoke(reason, now)` is called:

```
session.IsActive = false
session.RevokedAt = now
session.RevokedReason = reason

foreach token in session.Tokens where !token.IsRevoked:
    token.RevokedAt = now
    token.RevokedReason = reason
```

The revocation is **permanent** -- there is no "unrevoke" mechanism. The user must create a new session by logging in again.

### ISessionRevocationStore Blacklist

The `ISessionRevocationStore` maintains a cache-based blacklist so that outstanding access tokens (which are stateless JWTs) are rejected before their natural expiration:

| Method | Cache Key Pattern | Purpose |
|--------|-------------------|---------|
| `RevokeDeviceAsync` | `revoked:device:{userId}:{deviceId}` | Blacklists a specific device's tokens |
| `RevokeAllDevicesAsync` | `revoked:user:{userId}` | Blacklists all devices for a user |
| `IsDeviceRevokedAsync` | Checks `revoked:device:{userId}:{deviceId}` | Called during JWT validation |
| `IsUserRevokedAsync` | Checks `revoked:user:{userId}` | Called during JWT validation |

## Event: SessionRevokedEvent

Raised for each revoked session.

| Field | Type | Description |
|-------|------|-------------|
| `UserId` | `string` | The user's ID |
| `SessionId` | `string` | The revoked session's ID |
| `DeviceId` | `Guid` | The device associated with the session |
| `Reason` | `string` | "User logout" or "User logout all" |
| `OccurredAt` | `DateTime` | When the revocation occurred |

## Error Codes

| Code | HTTP | When |
|------|------|------|
| `User.NotFound` | 404 | Authenticated user not found in database |
| Validation error | 400 | `DeviceId` provided but is an empty GUID |

> The logout operation is idempotent for the most part. If the session for the specified device is already inactive, `RevokeSessionByDevice` silently returns without error. Similarly, `RevokeAllSession` only iterates active sessions.

## Source Files

- `src/core/OIO.Application/Context/UserContext/Commands/Logout/LogoutCommand.cs` (command + handler)
- `src/core/OIO.Domain/Context/UserContext/Aggregates/Users/User.cs` (RevokeSessionByDevice, RevokeAllSession)
- `src/core/OIO.Domain/Context/UserContext/Aggregates/Users/UserSession.cs` (Revoke)
- `src/core/OIO.Application/Context/UserContext/Services/ISessionRevocationStore.cs`
- `src/core/OIO.Domain/Context/UserContext/Errors/UserErrors.cs`
