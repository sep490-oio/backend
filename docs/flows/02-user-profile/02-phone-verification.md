# 02 - Phone Verification

## Overview

Phone verification is a two-step process: the user sets (or changes) their phone number, then confirms ownership by submitting an OTP code. The phone number is validated using the `libphonenumber` library and stored in E.164 format. OTP tokens are managed through `ISecureTokenStore` with the `TokenType.PhoneVerification` type.

---

## Sequence Diagram

```mermaid
sequenceDiagram
    autonumber
    actor Client
    participant SetAPI as PUT /api/me/phone
    participant SetHandler as SetPhoneNumberCommandHandler
    participant PhoneVO as PhoneNumber ValueObject
    participant DB as Database
    participant ConfirmAPI as POST /api/me/phone/confirm
    participant ConfirmHandler as ConfirmPhoneNumberCommandHandler
    participant TokenStore as ISecureTokenStore
    participant DomainEvents as Domain Events

    Client->>SetAPI: { phoneNumber: "0912345678", countryCode: "VN" }
    SetAPI->>SetHandler: SetPhoneNumberCommand

    SetHandler->>DB: Load User by current user ID
    DB-->>SetHandler: User entity

    SetHandler->>PhoneVO: PhoneNumber.Create("0912345678", "VN")
    PhoneVO->>PhoneVO: PhoneNumberUtil.Parse + IsValidNumber
    PhoneVO->>PhoneVO: Format to E.164 (+84912345678)
    PhoneVO-->>SetHandler: PhoneNumber value object

    SetHandler->>DB: user.SetPhoneNumber(phoneNumber, now)
    Note over DB: PhoneNumberConfirmed = false, PhoneNumberConfirmedAt = null

    SetHandler->>DB: SaveChangesAsync
    SetHandler-->>Client: 204 No Content

    Note over Client: OTP is sent out-of-band (SMS/notification)

    Client->>ConfirmAPI: { verificationCode: "123456" }
    ConfirmAPI->>ConfirmHandler: ConfirmPhoneNumberCommand

    ConfirmHandler->>DB: Load User by current user ID
    DB-->>ConfirmHandler: User entity

    alt PhoneNumber is null
        ConfirmHandler-->>Client: 403 User.PhoneNumber.NotSet
    end

    ConfirmHandler->>TokenStore: ValidateTokenAsync(PhoneVerification, userId, "123456")
    TokenStore-->>ConfirmHandler: true / false

    alt Token invalid
        ConfirmHandler-->>Client: 401 User.ConfirmationCode.Invalid
    end

    ConfirmHandler->>DB: user.ConfirmPhoneNumber(now)
    Note over DB: PhoneNumberConfirmed = true, PhoneNumberConfirmedAt = now

    ConfirmHandler->>DomainEvents: Raise UserPhoneConfirmedEvent
    ConfirmHandler->>DB: SaveChangesAsync
    ConfirmHandler-->>Client: 204 No Content
```

---

## Endpoints

### 1. PUT /api/me/phone

| Property | Value |
|----------|-------|
| Permission | `Me.ManagePhone` |
| Handler | `SetPhoneNumberCommandHandler` |
| Response | `204 No Content` |

**Request body:**

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `PhoneNumber` | `string` | Yes | -- | Phone number in local or international format |
| `CountryCode` | `string?` | No | `"VN"` | ISO country code for parsing (default region) |

**Command validation (`SetPhoneNumberCommand.Validate()`):**
- `PhoneNumber` -- must not be null or whitespace
- `CountryCode` -- when not null, must not be whitespace

**Handler steps:**
1. Load `User` by current user ID. Return `User.NotFound` if missing.
2. Create `PhoneNumber` value object via `PhoneNumber.Create(request.PhoneNumber, request.CountryCode)`:
   - Uses `PhoneNumberUtil.Parse()` with the provided `CountryCode` as default region
   - Validates with `PhoneNumberUtil.IsValidNumber()`
   - Formats to E.164 via `PhoneNumberUtil.Format(numberProto, PhoneNumberFormat.E164)`
   - Extracts the actual region code via `GetRegionCodeForNumber()`
3. Call `user.SetPhoneNumber(phoneNumber, now)`:
   - Sets `PhoneNumber` to the new value
   - Resets `PhoneNumberConfirmed = false`
   - Resets `PhoneNumberConfirmedAt = null`
4. Persist changes.

---

### 2. POST /api/me/phone/confirm

| Property | Value |
|----------|-------|
| Permission | `Me.ManagePhone` |
| Handler | `ConfirmPhoneNumberCommandHandler` |
| Response | `204 No Content` |

**Request body:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `VerificationCode` | `string` | Yes | OTP code received via SMS |

**Command validation (`ConfirmPhoneNumberCommand.Validate()`):**
- `VerificationCode` -- must not be whitespace

**Handler steps:**
1. Load `User` by current user ID. Return `User.NotFound` if missing.
2. Check `user.PhoneNumber is not null`. Return `User.PhoneNumber.NotSet` if null.
3. Validate the OTP via `ISecureTokenStore.ValidateTokenAsync(TokenType.PhoneVerification, userId, code)`. Return `User.ConfirmationCode.Invalid` if invalid.
4. Call `user.ConfirmPhoneNumber(now)`:
   - Sets `PhoneNumberConfirmed = true`
   - Sets `PhoneNumberConfirmedAt = now`
   - Raises `UserPhoneConfirmedEvent(UserId, PhoneNumber, OccurredAt)`
5. Persist changes.

---

## Phone Validation Details

The `PhoneNumber` value object (namespace `OIO.Domain.Context.UserContext.ValueObjects`) uses the `libphonenumber` library (`PhoneNumbers` NuGet package):

- **Default region:** `"VN"` (Vietnam) -- defined as `PhoneNumber.DefaultRegion`
- **Parsing:** `PhoneNumberUtil.Parse(value, defaultRegion)` handles both local (`0912345678`) and international (`+84912345678`) formats
- **Validation:** `PhoneNumberUtil.IsValidNumber(numberProto)` checks against the number plan for the detected region
- **Formatting:** Output is always in **E.164** format (e.g. `+84912345678`)
- **Region detection:** `GetRegionCodeForNumber()` stores the actual country code alongside the formatted number

---

## OTP Flow

The OTP lifecycle uses `ISecureTokenStore` with `TokenType.PhoneVerification`:

| Operation | Method | Description |
|-----------|--------|-------------|
| Create | `CreateTokenAsync(PhoneVerification, userId)` | Generates a token, stores its hash, returns plain token + TTL |
| Validate | `ValidateTokenAsync(PhoneVerification, userId, plainToken)` | Checks plain token against stored hash; returns bool |
| Invalidate | `InvalidateTokenAsync(PhoneVerification, userId)` | Deletes stored token after successful use |
| Rate limit | `HasActiveTokenAsync(PhoneVerification, userId)` | Checks if an unexpired token already exists |
| Cooldown | `GetTokenTtlAsync(PhoneVerification, userId)` | Returns remaining TTL for cooldown enforcement |

---

## Domain Event

### UserPhoneConfirmedEvent

| Field | Type | Description |
|-------|------|-------------|
| `UserId` | `string` | The user's ID |
| `PhoneNumber` | `string` | The confirmed E.164 phone number |
| `OccurredAt` | `DateTime` | Timestamp of confirmation |

Raised inside `User.ConfirmPhoneNumber()` after setting `PhoneNumberConfirmed = true`.

---

## Error Codes

| Code | HTTP Status | Condition |
|------|-------------|-----------|
| `User.NotFound` | 404 | Current user does not exist |
| `User.Deleted` | 403 | User account has been soft-deleted |
| `User.PhoneNumber.NotSet` | 403 | Attempting to confirm when no phone number is set |
| `User.ConfirmationCode.Invalid` | 401 | OTP code is invalid or expired |
| Format error (invariant) | 422 | Phone number fails `libphonenumber` parsing or validation |
