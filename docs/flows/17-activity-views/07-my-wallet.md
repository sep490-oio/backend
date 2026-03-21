# 17-07 -- My Wallet

## Overview

The wallet group provides 4 endpoints for users to view their financial data: wallet summary, transaction history, transaction detail, and withdrawal requests.

All 4 endpoints require only basic authentication (no granular permission).

---

## Endpoints

### 1. Get Wallet Summary

| Property | Value |
|----------|-------|
| Route | `GET /api/me/wallet` |
| Permission | (authenticated) |
| Tag | `Me` |
| Response | `WalletSummaryDto` |
| Errors | `401 Unauthorized`, `404 Not Found` |

#### WalletSummaryDto (7 fields)

| Field | Type | Description |
|-------|------|-------------|
| `walletId` | `Guid` | Wallet identifier |
| `currency` | `string` | Wallet currency code |
| `availableBalance` | `decimal` | Funds available for use (not held) |
| `pendingBalance` | `decimal` | Funds on hold (e.g. escrow, pending withdrawals) |
| `totalBalance` | `decimal` | `availableBalance + pendingBalance` |
| `isActive` | `bool` | Whether the wallet is active |
| `updatedAt` | `DateTime` | Last update timestamp |

---

### 2. Get Wallet Transactions

| Property | Value |
|----------|-------|
| Route | `GET /api/me/wallet/transactions` |
| Permission | (authenticated) |
| Tag | `Me` |
| Response | `PagedList<WalletTransactionDto>` |
| Errors | `400 Bad Request`, `401 Unauthorized` |

#### Filter Parameters

| Param | Type | Description |
|-------|------|-------------|
| `type` | `string?` | Filter by transaction type (see below) |
| `from` | `DateTime?` | Include transactions created at or after this timestamp |
| `to` | `DateTime?` | Include transactions created at or before this timestamp |
| `pageNumber` | `int?` | Page number (default 1) |
| `pageSize` | `int?` | Page size (default 10, max 50) |

**Default sort:** `CreatedAt` descending (newest first). No custom `sortBy` parameter.

#### Allowed `type` values

From `WalletTransactionType`:

| Type | Description |
|------|-------------|
| `credit` | Funds added to wallet |
| `debit` | Funds removed from wallet |
| `hold` | Funds placed on hold (e.g. bid deposit, escrow) |
| `release` | Held funds released back to available |

---

### 3. Get Transaction by ID

| Property | Value |
|----------|-------|
| Route | `GET /api/me/wallet/transactions/{transactionId}` |
| Permission | (authenticated) |
| Tag | `Me` |
| Response | `WalletTransactionDto` |
| Errors | `401 Unauthorized`, `404 Not Found` |

---

### 4. Get My Withdrawals

| Property | Value |
|----------|-------|
| Route | `GET /api/me/wallet/withdrawals` |
| Permission | (authenticated) |
| Tag | `Me` |
| Response | `PagedList<WithdrawalRequestDto>` |
| Errors | `400 Bad Request`, `401 Unauthorized` |

#### Filter Parameters

| Param | Type | Description |
|-------|------|-------------|
| `status` | `string?` | Filter by withdrawal status (see below) |
| `pageNumber` | `int?` | Page number (default 1) |
| `pageSize` | `int?` | Page size (default 10, max 50) |

**Default sort:** `CreatedAt` descending. No custom `sortBy` parameter.

#### Allowed `status` values

From `WithdrawalStatus`:

| Status | Description |
|--------|-------------|
| `pending` | Request submitted, awaiting review |
| `approved` | Approved by admin |
| `processing` | Bank transfer in progress |
| `completed` | Funds sent successfully |
| `rejected` | Rejected by admin |
| `cancelled` | Cancelled by user |

---

## WalletTransactionDto (10 fields)

| Field | Type | Description |
|-------|------|-------------|
| `id` | `Guid` | Transaction identifier |
| `type` | `string` | Transaction type (`credit`, `debit`, `hold`, `release`) |
| `amount` | `decimal` | Transaction amount |
| `currency` | `string` | Currency code |
| `balanceBefore` | `decimal` | Wallet balance before the transaction |
| `balanceAfter` | `decimal` | Wallet balance after the transaction |
| `description` | `string?` | Human-readable description |
| `referenceType` | `string?` | What entity this transaction is linked to (e.g. `order`, `withdrawal`) |
| `referenceId` | `Guid?` | ID of the referenced entity |
| `createdAt` | `DateTime` | Transaction timestamp |

---

## WithdrawalRequestDto (11 fields)

| Field | Type | Description |
|-------|------|-------------|
| `id` | `Guid` | Withdrawal request ID |
| `amount` | `decimal` | Requested withdrawal amount |
| `fee` | `decimal` | Processing fee |
| `netAmount` | `decimal` | Amount after fee (`amount - fee`) |
| `status` | `string` | Current status |
| `bankName` | `string?` | Destination bank name |
| `accountNumberMasked` | `string?` | Masked bank account number |
| `accountHolder` | `string?` | Bank account holder name |
| `rejectionReason` | `string?` | Reason for rejection (if rejected) |
| `createdAt` | `DateTime` | Request timestamp |
| `processedAt` | `DateTime?` | When the withdrawal was processed |

---

## Source References

| File | Path |
|------|------|
| Wallet Endpoint | `src/presentation/OIO.Api/Endpoints/UserContext/Me/GetMyWalletEndpoint.cs` |
| Transactions Endpoint | `src/presentation/OIO.Api/Endpoints/UserContext/Me/GetMyWalletTransactionsEndpoint.cs` |
| Transaction Detail Endpoint | `src/presentation/OIO.Api/Endpoints/UserContext/Me/GetMyWalletTransactionByIdEndpoint.cs` |
| Withdrawals Endpoint | `src/presentation/OIO.Api/Endpoints/UserContext/Me/GetMyWithdrawalsEndpoint.cs` |
| Transactions Query | `src/core/OIO.Application/Context/PaymentContext/Queries/GetMyWalletTransactions/GetMyWalletTransactionsQuery.cs` |
| Withdrawals Query | `src/core/OIO.Application/Context/PaymentContext/Queries/GetMyWithdrawals/GetMyWithdrawalsQuery.cs` |
| Summary DTO | `src/core/OIO.Application/Context/PaymentContext/DTOs/WalletSummaryDto.cs` |
| Transaction DTO | `src/core/OIO.Application/Context/PaymentContext/DTOs/WalletTransactionDto.cs` |
| Withdrawal DTO | `src/core/OIO.Application/Context/PaymentContext/DTOs/WithdrawalRequestDto.cs` |
| Transaction Type Enum | `src/core/OIO.Domain/Context/PaymentContext/Enums/WalletTransactionType.cs` |
| Withdrawal Status Enum | `src/core/OIO.Domain/Context/PaymentContext/Enums/WithdrawalStatus.cs` |
