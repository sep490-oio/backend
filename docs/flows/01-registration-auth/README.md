# Registration & Authentication Module

## Tong quan

Module Registration & Authentication cua OIO Auction Platform quan ly toan bo vong doi cua user tu luc dang ky den khi xac thuc, dang nhap, quan ly session, va bao mat 2FA. Module duoc xay dung theo kien truc Domain-Driven Design voi CQRS pattern (MediatR), su dung domain events de kich hoat cac side-effect nhu gui email, tao notification, va ghi audit log.

**Kien truc chinh:**
- **Domain Layer**: `User` aggregate root voi cac value objects (`UserEmail`, `UserName`, `Password`, `PersonName`) va entity con (`UserSession`, `UserRefreshToken`, `UserRole`, `UserLoginHistory`)
- **Application Layer**: Command handlers (CQRS), Event handlers (MediatR `INotificationHandler`)
- **Presentation Layer**: Minimal API endpoints (ASP.NET Core)
- **Infrastructure**: `ISecureTokenStore` (hash-based, TTL, one-time), `IJwtTokenProvider`, `IPasswordHasher`, `ISessionRevocationStore`

---

## 1. State Diagrams

### 1.1 User State Machine

```mermaid
---
config:
  layout: elk
---
stateDiagram-v2
    [*] --> Unregistered
    Unregistered --> Inactive : POST /api/auth/register<br/>User.Create() sets Status=Inactive
    Inactive --> Active : ConfirmEmail()<br/>auto-activates if Status==Inactive
    Active --> Authenticated : POST /api/auth/login<br/>password verified + session created
    Authenticated --> Active : POST /api/auth/logout<br/>session revoked
    Active --> Locked : Admin ChangeStatus("locked")<br/>UserStatusChangedEvent
    Active --> Banned : Admin ChangeStatus("banned")<br/>UserStatusChangedEvent
    Active --> Suspended : Admin ChangeStatus("suspended")<br/>UserStatusChangedEvent
    Active --> Inactive : SoftDelete()<br/>DeletedAt set + all sessions revoked
    Locked --> Active : Admin ChangeStatus("active")
    Banned --> Active : Admin ChangeStatus("active")
    Suspended --> Active : Admin ChangeStatus("active")
    Active --> LockedOut : 5 failed logins<br/>LockoutEnd = now + 30min
    LockedOut --> Active : LockoutEnd passed<br/>next login resets AccessFailedCount
```

**UserStatus enum values**: `active`, `inactive`, `locked`, `banned`, `suspended`

**RevokedStatus** (triggers session revocation via `UserStatusChangedEventHandler`): `banned`, `inactive`, `locked`, `suspended`

### 1.2 Session State Machine

```mermaid
---
config:
  layout: elk
---
stateDiagram-v2
    [*] --> Login : POST /api/auth/login<br/>or POST /api/auth/two-factor/verify
    Login --> ActiveSession : CreateSession()<br/>IsActive=true, ExpiresAt=now+sliding, AbsoluteExpiresAt=now+absolute
    ActiveSession --> Extended : RefreshToken rotation<br/>ExtendSlidingExpiration(), clamped to AbsoluteExpiresAt
    Extended --> ActiveSession : Still within sliding window
    ActiveSession --> SlidingExpired : ExpiresAt <= now<br/>Revoke("Sliding expiration reached")
    ActiveSession --> AbsoluteExpired : AbsoluteExpiresAt <= now<br/>Revoke("Absolute expiration reached")
    ActiveSession --> Revoked : Manual logout / password change /<br/>device mismatch / token reuse / max families exceeded
    Extended --> SlidingExpired : ExpiresAt <= now
    Extended --> AbsoluteExpired : AbsoluteExpiresAt <= now
    SlidingExpired --> Purged : CleanupJob (90 days)<br/>ExecuteDeleteAsync
    AbsoluteExpired --> Purged : CleanupJob (90 days)
    Revoked --> Purged : CleanupJob (90 days)
```

**Key constants**:
- Max active sessions (families): `5` (MaxTokenFamilies)
- Sliding expiration: configurable via `ITokenExpirationSettings.FamilySlidingExpiration`
- Absolute expiration: configurable via `ITokenExpirationSettings.FamilyAbsoluteExpiration`
- Cleanup job interval: every 6 hours
- Purge threshold: 90 days after creation

### 1.3 2FA State Machine

```mermaid
---
config:
  layout: elk
---
stateDiagram-v2
    [*] --> Disabled : TwoFactorEnabled=false<br/>TwoFactorProvider=None
    Disabled --> SetupPending : POST /api/me/two-factor/setup<br/>SetupTotp() stores PendingTwoFactorSecret
    SetupPending --> Enabled : POST /api/me/two-factor/confirm<br/>ConfirmTotpSetup() moves Pending→Active<br/>TwoFactorEnabled=true, Provider=Totp
    SetupPending --> Disabled : Never confirmed / new setup overwrites
    Enabled --> Disabled : POST /api/me/two-factor/disable<br/>DisableTwoFactor() clears all 2FA fields
```

**2FA providers** (TwoFactorProvider enum): `none`, `sms`, `email`, `totp`

### 1.4 Login Flow Overview (Normal + 2FA Branch)

```mermaid
sequenceDiagram
    participant C as Client
    participant API as POST /api/auth/login
    participant DB as Database
    participant Cache as RevocationStore

    C->>API: { Account, Password, DeviceId }
    API->>DB: Find user by normalized email or username
    alt User not found
        API-->>C: 401 User.Credentials.Invalid
    end
    API->>API: EnsureNotLockedOut(now)
    alt Locked out
        API->>DB: RecordFailedLogin()
        API-->>C: 403 User.Locked
    end
    API->>API: Check Status (Locked → 403, Inactive → 403)
    API->>API: Verify password hash
    alt Password invalid
        API->>DB: RecordFailedLogin() → AccessFailedCount++
        Note over DB: If count >= 5 → LockoutEnd = now+30min<br/>Raise UserLockedOutEvent
        API-->>C: 401 User.Credentials.Invalid
    end
    API->>DB: RecordSuccessfulLogin() → reset AccessFailedCount
    alt 2FA enabled (TOTP)
        API->>DB: SaveChanges (login history)
        API->>API: GenerateTwoFactorJwt(userId, now)
        API-->>C: 200 { AccessToken: limited_jwt, RequiresTwoFactor: true, RefreshToken: "" }
        Note over C: Limited JWT expires in 3 minutes<br/>Purpose: 2fa_verification
    else Normal login
        API->>DB: CreateSession() → revoke same device, enforce max 5
        API->>DB: CreateRefreshToken(hashedToken)
        API->>API: GenerateJwt(userId, email, userName, deviceId, roles)
        API->>DB: SaveChanges
        API->>Cache: ClearDeviceRevocation + ClearUserRevocation
        API-->>C: 200 AuthTokenDto { AccessToken, RefreshToken, Session }
    end
```

### 1.5 Token Rotation Overview (Device Mismatch + Reuse Detection)

```mermaid
sequenceDiagram
    participant C as Client
    participant API as POST /api/auth/refresh
    participant DB as Database
    participant Cache as RevocationStore

    C->>API: { RefreshToken, DeviceId } + Bearer AccessToken
    API->>DB: Load user by currentUser.UserId (from JWT)
    alt DeviceId mismatch (request vs JWT)
        API->>DB: RevokeAllSession("device id mismatch")
        API->>Cache: RevokeAllDevicesAsync
        API-->>C: 401 User.Session.Token.Revoked
    end
    API->>API: Hash(RefreshToken) → find matching token in sessions
    alt Token not found
        API-->>C: 401 User.Session.Token.Invalid
    end
    alt Session.DeviceId != request.DeviceId
        API->>DB: RevokeSession("Device mismatch: expected X, got Y")
        API-->>C: 401 User.DeviceMismatch
    end
    API->>DB: RotateRefreshToken(currentToken, newHash)
    alt Token already used (reuse detected)
        API->>DB: Revoke entire session ("Token reuse detected")
        API-->>C: 403 Auth.Session.Compromised
    end
    alt Token revoked or expired
        API-->>C: 401 appropriate error
    end
    API->>API: Mark old token as used
    API->>DB: ExtendSlidingExpiration (clamped to AbsoluteExpiresAt)
    API->>API: Create new refresh token + new access JWT
    API->>DB: SaveChanges
    API-->>C: 200 AuthTokenDto { new AccessToken, new RefreshToken, Session }
```

### 1.6 Registration + Email Verification

```mermaid
sequenceDiagram
    participant C as Client
    participant API as API
    participant Domain as User Aggregate
    participant Handler as UserCreatedEventHandler
    participant Store as ISecureTokenStore
    participant Mail as IUserMailNotifier

    C->>API: POST /api/auth/register<br/>{ UserName, Email, Password, Currency, FirstName?, LastName? }
    API->>API: Validate (email regex, username 3-50 chars, password 8-128 chars)
    API->>Domain: UserEmail.Create, UserName.Create, Password.Create(hash)
    API->>API: Check duplicate email + username in DB
    API->>Domain: User.Create(userName, email, now, personName, currency, password)
    Note over Domain: Status=Inactive, LockoutEnabled=true<br/>Init Profile + Init Wallet<br/>Raise UserCreatedEvent
    API->>Domain: AssignRole("bidder") + AssignRole("user")
    API->>API: SaveChanges → dispatches UserCreatedEvent
    API-->>C: 201 Created { UserDto }
    Handler->>Store: CreateTokenAsync(EmailVerification, userId)
    Store-->>Handler: (plainToken, ttl)
    Handler->>Mail: SendWelcomeVerifyAsync(email, userName, userId, token, expiry)
    C->>API: POST /api/auth/confirm-email { UserId, Token }
    API->>Store: ValidateTokenAsync(EmailVerification, userId, token)
    API->>Domain: ConfirmEmail(now) → EmailConfirmed=true
    Note over Domain: If Status==Inactive → ChangeStatus(Active)<br/>Raise UserStatusChangedEvent
    API->>Store: InvalidateTokenAsync(EmailVerification, userId)
    API-->>C: 204 No Content
```

### 1.7 TOTP 2FA Setup Flow

```mermaid
sequenceDiagram
    participant C as Client
    participant API as API
    participant Domain as User Aggregate
    participant TOTP as ITotpService
    participant DB as Database

    C->>API: POST /api/me/two-factor/setup<br/>(Requires auth)
    API->>DB: Load user by currentUser.UserId
    API->>TOTP: GenerateSecret() → base32 secret
    API->>Domain: SetupTotp(secret, now)
    Note over Domain: Requires EmailConfirmed==true<br/>Stores PendingTwoFactorSecret
    API->>TOTP: GenerateQrCodePng(secret, email, appName)
    API->>DB: SaveChanges
    API-->>C: 200 { SharedKey, QrCodeBase64 }
    Note over C: User scans QR code in authenticator app
    C->>API: POST /api/me/two-factor/confirm { Code }
    API->>DB: Load user
    API->>API: Check PendingTwoFactorSecret exists
    API->>TOTP: VerifyCode(pendingSecret, code)
    alt Code invalid
        API-->>C: 401 User.Totp.InvalidCode
    end
    API->>Domain: ConfirmTotpSetup(now)
    Note over Domain: TwoFactorSecret = PendingTwoFactorSecret<br/>PendingTwoFactorSecret = null<br/>TwoFactorEnabled = true<br/>TwoFactorProvider = Totp
    API->>DB: Delete existing recovery codes
    API->>DB: Generate 8 recovery codes (5 bytes hex each, hashed)
    API->>DB: SaveChanges
    API-->>C: 200 { RecoveryCodes: [...] }
```

---

## 2. Subflow Index

| # | File | Mo ta |
|---|------|-------|
| 01 | [01-registration.md](./01-registration.md) | Dang ky tai khoan moi |
| 02 | [02-email-verification.md](./02-email-verification.md) | Xac thuc email + gui lai email |
| 03 | [03-login.md](./03-login.md) | Dang nhap (normal + 2FA branch) |
| 04 | 04-token-refresh.md | Refresh token rotation |
| 05 | 05-logout.md | Dang xuat (single device / all) |
| 06 | 06-password-reset.md | Quen mat khau + reset |
| 07 | 07-change-password.md | Doi mat khau (authenticated) |
| 08 | 08-two-factor-setup.md | Cai dat TOTP 2FA |
| 09 | 09-session-management.md | Quan ly sessions |
| 10 | 10-security-reference.md | Tham khao bao mat |

---

## 3. Domain Events & Handlers

| Domain Event | Handler | Hanh dong |
|-------------|---------|-----------|
| `UserCreatedEvent` | `UserCreatedEventHandler` | Tao email verification token (ISecureTokenStore) + gui welcome email (SendWelcomeVerifyAsync) |
| `EmailVerificationRequestedEvent` | `EmailVerificationRequestedEventHandler` | Kiem tra cooldown (ResendEmailCooldown=60s) + tao token + gui email xac thuc (SendResendVerifyAsync) |
| `UserEmailConfirmedEvent` | `UserEmailConfirmedEventHandler` | Tao notification "Email da duoc xac thuc" + gui email xac nhan (SendEmailConfirmedAsync) |
| `UserStatusChangedEvent` | `UserStatusChangedEventHandler` | Ghi AuditLog + tao notification + gui email thong bao + revoke all sessions neu status la RevokedStatus |
| `LoginAttemptedEvent` | `LoginAttemptedEventHandler` | [Success] Detect suspicious login from new IP → MonitoringAlert + notification. [Failed] Track failed login rate per IP (cache 1h) → brute force alert at threshold 5 |
| `UserLockedOutEvent` | `UserLockedOutEventHandler` | Ghi AuditLog + tao notification "Tai khoan tam thoi bi khoa" (Urgent) + gui email alert |
| `SessionRevokedEvent` | `SessionRevokedEventHandler` | Ghi AuditLog + tao notification (High priority neu security-sensitive: reuse/mismatch/theft) + gui email alert |
| `SessionNearingExpirationEvent` | `SessionNearingExpirationEventHandler` | Tao notification "Phien dang nhap sap het han" |
| `UserPasswordChangedEvent` | `UserPasswordChangedEventHandler` | Gui email alert + revoke all sessions (ISessionRevocationStore.RevokeAllDevicesAsync) |
| `PasswordResetRequestedEvent` | `PasswordResetRequestedEventHandler` | Kiem tra cooldown + tao password reset token + gui email reset |
| `RefreshTokenRotatedEvent` | _(no dedicated handler)_ | Raised khi token rotation thanh cong |
| `VerificationSubmittedEvent` | `VerificationSubmittedEventHandler` | Xu ly eKYC verification (khong lien quan truc tiep den auth) |

---

## 4. Background Jobs

| Job | Class | Interval | Mo ta |
|-----|-------|----------|-------|
| Expired Session Cleanup | `ExpiredSessionCleanupJob` | 6 gio | 1. Revoke sessions qua absolute expiration<br/>2. Revoke sessions qua sliding expiration<br/>3. Revoke orphaned tokens trong revoked sessions<br/>4. Hard delete sessions inactive > 90 ngay |

---

## 5. Tong hop Endpoints

### Auth Endpoints (`/api/auth/*`)

| # | Method | Route | Auth | Response | Mo ta |
|---|--------|-------|------|----------|-------|
| 1 | POST | `/api/auth/register` | Anonymous | 201 UserDto | Dang ky tai khoan |
| 2 | POST | `/api/auth/login` | Anonymous | 200 AuthTokenDto | Dang nhap |
| 3 | POST | `/api/auth/logout` | ExpiredTokenAllowed | 204 | Dang xuat (DeviceId? → single/all) |
| 4 | POST | `/api/auth/refresh` | ExpiredTokenAllowed | 200 AuthTokenDto | Refresh token rotation |
| 5 | POST | `/api/auth/confirm-email` | Anonymous | 204 | Xac thuc email |
| 6 | POST | `/api/auth/resend-confirm-email` | Anonymous | 204 | Gui lai email xac thuc |
| 7 | POST | `/api/auth/forgot-password` | Anonymous | 204 | Yeu cau reset mat khau |
| 8 | POST | `/api/auth/reset-password` | Anonymous | 204 | Reset mat khau bang token |
| 9 | POST | `/api/auth/two-factor/verify` | RequireAuthorization | 200 AuthTokenDto | Xac thuc TOTP khi dang nhap |

### Me Endpoints (`/api/me/*`) - Lien quan den Auth

| # | Method | Route | Auth | Response | Mo ta |
|---|--------|-------|------|----------|-------|
| 10 | PUT | `/api/me/password` | RequireAuthorization | 204 | Doi mat khau |
| 11 | POST | `/api/me/two-factor/enable` | RequireAuthorization | 204 | Bat 2FA (provider: sms/email/totp) |
| 12 | POST | `/api/me/two-factor/disable` | RequireAuthorization | 204 | Tat 2FA |
| 13 | POST | `/api/me/two-factor/setup` | RequireAuthorization | 200 SetupTotpResponse | Khoi tao TOTP (QR code) |
| 14 | POST | `/api/me/two-factor/confirm` | RequireAuthorization | 200 ConfirmTotpSetupResponse | Xac nhan TOTP setup + nhan recovery codes |
| 15 | GET | `/api/me/sessions` | RequireAuthorization | 200 List | Xem sessions dang hoat dong |
| 16 | GET | `/api/me/login-history` | RequireAuthorization | 200 List | Xem lich su dang nhap |
| 17 | POST | `/api/me/two-factor/recovery-codes` | RequireAuthorization | 200 | Tao lai recovery codes |
