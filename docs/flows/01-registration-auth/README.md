# 01 - Registration & Authentication

## Tong quan

Module xu ly toan bo luong dang ky, xac thuc va quan ly phien dang nhap cua nguoi dung.
Su dung JWT access token + refresh token voi co che token rotation va session management.

## State Machine - Trang thai nguoi dung

```
[Unregistered]
      |
      | POST /api/auth/register
      v
[Registered / Email Unconfirmed]
      |
      | POST /api/auth/confirm-email
      v
[Active / Email Confirmed]
      |
      +-- POST /api/auth/login --> [Authenticated]  (2FA tat)
      |                               |
      |                               +-- POST /api/auth/refresh --> [Token Refreshed]
      |                               |
      |                               +-- POST /api/auth/logout --> [Session Ended]
      |
      +-- POST /api/auth/login --> [Pending 2FA]  (2FA bat)
      |                               |
      |                               +-- POST /api/auth/two-factor/verify --> [Authenticated]
      |                               |
      |                               +-- (3 phut het han) --> [Login Expired]
      |
      +-- (Admin) --> [Locked]
      |                  |
      |                  +-- (Admin unlock) --> [Active]
      |
      +-- (Admin) --> [Inactive]
```

## State Machine - Phien dang nhap (Session)

```
[Login]
   |
   | CreateSession (deviceId, userAgent, ipAddress)
   v
[Active Session]
   |
   +-- Refresh Token --> [Session Extended] (sliding expiration reset)
   |
   +-- Sliding Expiration reached --> [Expired]
   |
   +-- Absolute Expiration reached --> [Expired]
   |
   +-- Logout / Device mismatch --> [Revoked]
   |
   +-- (Cleanup Job moi 6h) --> [Purged after 90 days]
```

## Cac subflow

| # | Subflow | File |
|---|---------|------|
| 1 | Dang ky tai khoan | [registration.md](./registration.md) |
| 2 | Xac thuc email | [email-verification.md](./email-verification.md) |
| 3 | Dang nhap | [login.md](./login.md) |
| 4 | Lam moi token | [refresh-token.md](./refresh-token.md) |
| 5 | Quen & dat lai mat khau | [forgot-reset-password.md](./forgot-reset-password.md) |
| 6 | Xac thuc hai yeu to | [two-factor-auth.md](./two-factor-auth.md) |
| 7 | Quan ly phien | [session-management.md](./session-management.md) |

## Domain Events

| Event | Khi nao |
|-------|---------|
| `UserCreatedEvent` | Dang ky thanh cong |
| `UserEmailConfirmedEvent` | Xac thuc email thanh cong |
| `LoginAttemptedEvent` | Moi lan dang nhap (thanh cong hoac that bai) |
| `RefreshTokenRotatedEvent` | Refresh token duoc rotate |
| `SessionRevokedEvent` | Session bi thu hoi (logout / device mismatch) |
| `UserLockedOutEvent` | Tai khoan bi khoa do login sai nhieu lan |
| `UserPasswordChangedEvent` | Mat khau bi thay doi |
| `SessionNearingExpirationEvent` | Session sap het han absolute |

## Background Jobs

- **ExpiredSessionCleanupJob**: Chay moi 6 gio, don dep session het han (sliding/absolute), thu hoi token mo coi, xoa session cu hon 90 ngay.
