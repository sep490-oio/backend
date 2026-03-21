# 06 - Platform Wallet

## Endpoint

| | |
|---|---|
| **Method** | `GET` |
| **URL** | `/api/admin/payments/platform-wallet` |
| **Permission** | `ReadPayments` |
| **Handler** | `GetPlatformWalletQueryHandler` |
| **Response** | `WalletSummaryDto` |

## Overview

The platform wallet is a special singleton `Wallet` that holds funds belonging to the OIO platform rather than any individual user. It receives credits from escrow settlements and deposit forfeitures.

## Creation

The platform wallet is created at application startup by `DatabaseSeeder.SeedPlatformWalletAsync`:

```
Wallet.CreatePlatformWallet(Currency.Vnd, clock.UtcNow)
```

Key properties:
- `UserId = null` (no owning user)
- `Type = WalletType.Platform`
- `IsActive = true`
- `WalletFunds = WalletFunds.Empty(Currency.Vnd)` (starts with zero balance)
- Domain events are cleared after seeding (not published)

## Characteristics

| Property | Value |
|----------|-------|
| `UserId` | `null` |
| `Type` | `Platform` |
| `IsActive` | `true` (always) |
| **Uniqueness** | Enforced by unique index on `type = 'platform'` -- only one platform wallet can exist |

## Revenue Sources

| Source | Operation | When |
|--------|-----------|------|
| Escrow settlement | `Credit` | Escrow released to platform (platform fee portion) |
| Deposit forfeit | `Credit` | Bidder deposit forfeited (e.g., winner fails to pay) |

## Query Handler

`GetPlatformWalletQueryHandler`:
1. Queries `Wallet` where `Type == WalletType.Platform` (with `AsNoTracking`)
2. Includes `WalletTransactions` for full transaction history
3. Maps to `WalletSummaryDto` via `PaymentReadModelMapper.ToSummaryDto`
4. Returns 404 if no platform wallet exists (should never happen after seeding)

## Notes

- The platform wallet behaves like a regular wallet in terms of operations (Credit, Debit, Hold, etc.) but domain events from `Credit`/`Debit` are only raised when `UserId` is non-null -- so the platform wallet does **not** raise `WalletCreditedDomainEvent` or `WalletDebitedDomainEvent`.
- The seeder checks for existence before creating, making it idempotent across restarts.
