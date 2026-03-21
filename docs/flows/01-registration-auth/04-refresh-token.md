# 04 - Refresh Token Rotation

## Endpoint

| Method | Path | Auth |
|--------|------|------|
| `POST` | `/api/auth/refresh` | Requires valid access token (JWT) |

## Request

```json
{
  "refreshToken": "string",
  "deviceId": "guid",
  "ipAddress": "string (IPAddress)"
}
```

**Validation** (`IHasValidate`):
- `RefreshToken` - must not be whitespace
- `DeviceId` - must not be an empty GUID

## Response (`AuthTokenDto`)

```json
{
  "accessToken": "string",
  "refreshToken": "string (new rotated token)",
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

## Token Rotation Sequence Diagram

```mermaid
sequenceDiagram
    participant C as Client
    participant H as RefreshTokenCommandHandler
    participant U as User Aggregate
    participant S as UserSession
    participant RS as ISessionRevocationStore
    participant DB as Database

    C->>H: POST /api/auth/refresh (RefreshToken, DeviceId, IpAddress)

    H->>DB: Load User (with Sessions, Tokens, Roles)
    alt User not found
        H-->>C: Error: User.Session.Token.Invalid
    end

    Note over H: Step 1 - Device ID mismatch (request vs access token)
    alt request.DeviceId != currentUser.DeviceId
        H->>U: RevokeAllSession("device id mismatch")
        U->>S: Revoke each active session
        S->>S: IsActive = false, revoke all tokens
        U-->>H: SessionRevokedEvent (per session)
        H->>RS: RevokeAllDevicesAsync(userId)
        H->>DB: SaveChanges
        H-->>C: Error: User.Session.Token.Revoked
    end

    Note over H: Step 2 - Find token by hash
    H->>H: Hash(request.RefreshToken)
    H->>U: Search all sessions for matching TokenHash
    alt Token not found
        H-->>C: Error: User.Session.Token.Invalid
    end

    Note over H: Step 3 - Session-device match
    alt session.DeviceId != request.DeviceId
        H->>U: RevokeSession(sessionId, "Device mismatch")
        U->>S: Revoke session + all its tokens
        U-->>H: SessionRevokedEvent
        H->>DB: SaveChanges
        H-->>C: Error: User.DeviceMismatch
    end

    Note over H: Step 4 - Rotate token
    H->>H: Generate new raw refresh token + hash
    H->>U: RotateRefreshToken(sessionId, currentToken, newHash, ...)
    U->>S: RotateToken(currentToken, newHash, ...)

    alt Token reuse detected (currentToken.IsUsed == true)
        S->>S: Revoke entire session ("Token reuse detected")
        S-->>U: Return Error: Auth.Session.Compromised
        H->>DB: SaveChanges
        H-->>C: Error: Auth.Session.Compromised (403)
    end
    alt Token already revoked
        S-->>H: Error: User.Session.Token.Revoked
    end
    alt Token expired
        S-->>H: Error: User.Session.Token.Expired
    end

    S->>S: currentToken.MarkAsUsed(now)
    S->>S: ExtendSlidingExpiration (clamp to AbsoluteExpiresAt)
    S->>S: ClampToAbsoluteExpiration for new token TTL
    S->>S: Create new UserRefreshToken (counter + 1)
    S-->>U: Return new token

    U-->>H: RefreshTokenRotatedEvent

    Note over H: Step 5 - Check nearing expiration
    alt session.RemainingAbsoluteTime < 1 day
        U-->>H: SessionNearingExpirationEvent
    end

    Note over H: Step 6 - Generate new access JWT
    H->>H: GenerateJwt(userId, email, userName, deviceId, roles)

    H->>DB: SaveChanges
    H-->>C: AuthTokenDto (new access + refresh tokens)
```

## Token Family Chain

```mermaid
flowchart TD
    S["UserSession<br/>(DeviceId, SlidingExpiry, AbsoluteExpiry)"]

    T0["Token #0<br/>ParentTokenId: null<br/>RotationCounter: 0<br/>IsUsed: true"]
    T1["Token #1<br/>ParentTokenId: Token#0.Id<br/>RotationCounter: 1<br/>IsUsed: true"]
    T2["Token #2<br/>ParentTokenId: Token#1.Id<br/>RotationCounter: 2<br/>IsUsed: true"]
    T3["Token #3 (current)<br/>ParentTokenId: Token#2.Id<br/>RotationCounter: 3<br/>IsUsed: false"]

    S --> T0
    T0 -->|"rotate"| T1
    T1 -->|"rotate"| T2
    T2 -->|"rotate"| T3

    REUSE["Attacker replays Token #1"]
    REUSE -.->|"IsUsed == true"| REVOKE["Entire session REVOKED<br/>(all tokens in family)"]

    style REUSE fill:#ff6b6b,color:#fff
    style REVOKE fill:#cc0000,color:#fff
    style T3 fill:#51cf66,color:#fff
```

## Full Flow (7 Steps)

| Step | Action | Details |
|------|--------|---------|
| 1 | **Get user** | Load `User` with `Sessions` (including `Tokens`) and `Roles`. Return `User.Session.Token.Invalid` if not found. |
| 2 | **Device mismatch check (access token)** | Compare `request.DeviceId` with `ICurrentUser.DeviceId` (from JWT). If different, revoke ALL sessions via `RevokeAllSession()`, blacklist all devices in `ISessionRevocationStore`, return `User.Session.Token.Revoked`. |
| 3 | **Find token** | Hash the incoming refresh token via `ITokenHasher.Hash()`. Iterate all sessions and their tokens looking for a matching `TokenHash`. Return `User.Session.Token.Invalid` if not found. |
| 4 | **Session-device match** | Compare `session.DeviceId` with `request.DeviceId`. If different, revoke that single session via `RevokeSession()`, return `User.DeviceMismatch`. |
| 5 | **Rotation** | Call `User.RotateRefreshToken()` which delegates to `UserSession.RotateToken()`. Inside rotation: **(a)** reuse check -- if `currentToken.IsUsed`, revoke entire session and return `Auth.Session.Compromised`; **(b)** check revoked/expired; **(c)** mark old token as used (`MarkAsUsed`); **(d)** extend sliding expiration, clamped to `AbsoluteExpiresAt`; **(e)** clamp new token's TTL to not exceed `AbsoluteExpiresAt`; **(f)** create new `UserRefreshToken` with `RotationCounter + 1` and `ParentTokenId` pointing to old token. Raises `RefreshTokenRotatedEvent`. |
| 6 | **Nearing expiration check** | If `session.RemainingAbsoluteTime(now) < 1 day` and session is not already absolute-expired, raise `SessionNearingExpirationEvent` with `AbsoluteExpiresAt` and `RemainingTime`. |
| 7 | **New JWT** | Generate a new access token JWT with `userId`, `email`, `userName`, `deviceId`, and `roles`. Return `AuthTokenDto` with both new tokens and session metadata. |

## Anti-Theft Mechanisms

### 1. Device ID Binding (Two-Layer)

- **Layer 1 -- Access token device check**: The `DeviceId` in the request body is compared against `ICurrentUser.DeviceId` extracted from the JWT. A mismatch means the refresh token was sent with someone else's access token. **Action**: revoke ALL user sessions, blacklist via `ISessionRevocationStore.RevokeAllDevicesAsync()`.
- **Layer 2 -- Session device check**: The `DeviceId` in the request is compared against `session.DeviceId`. A mismatch means the token somehow ended up on a different device. **Action**: revoke that single session.

### 2. Token Reuse Detection

Each `UserRefreshToken` has an `IsUsed` flag. When a token is rotated, the old token is marked `IsUsed = true`. If a previously-used token is presented again:
- The entire session (token family) is revoked via `session.Revoke("Token reuse detected")`.
- All tokens in the session are revoked.
- Returns `Auth.Session.Compromised` (HTTP 403).

This catches the scenario where an attacker steals a refresh token but the legitimate user has already used it.

### 3. Session Expiration (Sliding + Absolute)

- **Sliding expiration**: Extended on each rotation by `FamilySlidingExpiration`, clamped to never exceed `AbsoluteExpiresAt`.
- **Absolute expiration**: Hard maximum session lifetime (`FamilyAbsoluteExpiration`). Once reached, session is revoked with reason "Absolute expiration reached -- re-authentication required".
- New refresh token TTL is also clamped: if `now + RefreshTokenExpiration > AbsoluteExpiresAt`, the token gets a shorter TTL.

### 4. Token Hashing

Refresh tokens are never stored in plaintext. `ITokenHasher.Hash()` is used before storage, and incoming tokens are hashed before comparison.

### 5. Session Revocation Store (Blacklist)

`ISessionRevocationStore` maintains a cache-based blacklist (keys like `revoked:device:{userId}:{deviceId}` and `revoked:user:{userId}`). When sessions are revoked during refresh, corresponding entries are added so that any outstanding access tokens for those devices are immediately rejected.

## SessionNearingExpirationEvent

Raised when `session.RemainingAbsoluteTime(now) < TimeSpan.FromDays(1)` and the session has not yet absolute-expired. Contains:

| Field | Type | Description |
|-------|------|-------------|
| `UserId` | `string` | The user's ID |
| `SessionId` | `string` | The session approaching expiration |
| `AbsoluteExpiresAt` | `DateTime` | When the session will hard-expire |
| `RemainingTime` | `TimeSpan` | Time remaining until absolute expiration |
| `OccurredAt` | `DateTime` | When the event was raised |

This event allows the system to notify the user that their session is about to expire and they will need to re-authenticate.

## Error Codes

| Code | HTTP | When |
|------|------|------|
| `User.Session.Token.Invalid` | 401 | User not found, or refresh token hash not found in any session |
| `User.Session.Token.Revoked` | 401 | Request DeviceId mismatches access token DeviceId (all sessions revoked); or token was already revoked |
| `User.DeviceMismatch` | 401 | Session's DeviceId mismatches request DeviceId (single session revoked) |
| `Auth.Session.Compromised` | 403 | Token reuse detected -- entire session revoked |
| `User.Session.Token.Expired` | 401 | Refresh token has expired |
| `User.Session.Token.NotIn` | 401 | Token does not belong to the session |
| `User.Session.Inactive` | 401 | Session is no longer active |
| `User.Session.Expired` | 403 | Session sliding or absolute expiration reached |
| `User.Deleted` | 403 | User has been soft-deleted |
| `User.Locked` | 403 | User account is locked out |

## Source Files

- `src/core/OIO.Application/Context/UserContext/Commands/RefreshToken/RefreshTokenCommand.cs`
- `src/core/OIO.Application/Context/UserContext/Commands/RefreshToken/RefreshTokenCommandHandler.cs`
- `src/core/OIO.Domain/Context/UserContext/Aggregates/Users/User.cs` (RotateRefreshToken, CreateRefreshToken)
- `src/core/OIO.Domain/Context/UserContext/Aggregates/Users/UserSession.cs` (RotateToken, ExtendSlidingExpiration, ClampToAbsoluteExpiration)
- `src/core/OIO.Domain/Context/UserContext/Aggregates/Users/UserRefreshToken.cs`
- `src/core/OIO.Domain/Context/UserContext/Errors/UserErrors.cs`
- `src/core/OIO.Application/Context/UserContext/Services/ISessionRevocationStore.cs`
- `src/core/OIO.Application/Context/UserContext/Services/ITokenExpirationSettings.cs`
