# 03 - Create Withdrawal Request

## Endpoint

| | |
|---|---|
| **Method** | `POST` |
| **URL** | `/api/me/wallet/withdrawals` |
| **Auth** | Authenticated user |
| **Handler** | `CreateWithdrawalRequestCommandHandler` |

## Request Body

```json
{
  "amount": 500000,
  "bankName": "Vietcombank",
  "accountNumber": "1234567890",
  "accountHolder": "NGUYEN VAN A"
}
```

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `Amount` | `decimal` | Yes | Must be positive; must not exceed AvailableBalance |
| `BankName` | `string` | Yes | Trimmed |
| `AccountNumber` | `string` | Yes | Trimmed |
| `AccountHolder` | `string` | Yes | Trimmed |

## Handler Flow

```mermaid
sequenceDiagram
    actor User
    participant API as CreateWithdrawalRequestHandler
    participant Wallet
    participant WR as WithdrawalRequest

    User->>API: POST /api/me/wallet/withdrawals
    API->>API: Get current UserId
    API->>Wallet: Find active wallet (UserId, IsActive=true)
    alt Wallet not found
        API-->>User: 404 Wallet.NotFound
    end
    API->>API: Calculate fee (currently 0)
    API->>Wallet: wallet.Hold(amount, description="Withdrawal hold - {amount}")
    alt Insufficient balance
        API-->>User: 400 Balance validation error
    end
    Wallet-->>API: WalletHeldDomainEvent
    API->>WR: WithdrawalRequest.Create(userId, walletId, amount, fee, bankAccount, now)
    WR-->>API: WithdrawalRequestCreatedDomainEvent
    API->>API: SaveChanges
    API-->>User: 200 CreateWithdrawalRequestResponse
```

## BankAccount Value Object

Created via `BankAccount.Create(bankName, accountNumber, accountHolder)`. All fields are trimmed. Used as an owned entity on `WithdrawalRequest`.

| Property | Type | Nullable |
|----------|------|----------|
| `BankName` | `string?` | Yes |
| `AccountNumber` | `string?` | Yes |
| `AccountHolder` | `string?` | Yes |

## Response

```json
{
  "withdrawalRequestId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "amount": 500000,
  "fee": 0,
  "netAmount": 500000,
  "status": "pending"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `WithdrawalRequestId` | `Guid` | New withdrawal request ID |
| `Amount` | `decimal` | Requested withdrawal amount |
| `Fee` | `decimal` | Withdrawal fee (currently always 0) |
| `NetAmount` | `decimal` | `Amount - Fee` |
| `Status` | `string` | Always `"pending"` on creation |

## Key Behaviors

- **Fee calculation:** Currently hardcoded to `0`. The handler has a placeholder for configurable fees.
- **Hold:** The full `Amount` is held (moved from AvailableBalance to PendingBalance) immediately on creation. This ensures funds are reserved.
- **NetAmount:** `Amount - Fee`. This is the amount the user will actually receive in their bank account.
- **Domain events raised:**
  - `WalletHeldDomainEvent` (from `wallet.Hold`)
  - `WithdrawalRequestCreatedDomainEvent` (from `WithdrawalRequest.Create`)
