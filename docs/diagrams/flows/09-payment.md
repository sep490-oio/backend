# Payment Flow

## VNPay Payment Sequence

```mermaid
sequenceDiagram
    autonumber
    participant Buyer
    participant API
    participant DB
    participant VNPay

    Buyer->>API: POST /payments/vnpay/create-url (orderId, amount)
    API->>DB: Create Transaction(status=Pending, type=Payment, method=vnpay)
    API->>API: VnPayHelper.BuildPaymentUrl(txnRef, amount, returnUrl, ipnUrl)
    API-->>Buyer: {paymentUrl}

    Buyer->>VNPay: Redirect to VNPay payment page
    Note over Buyer,VNPay: Buyer completes payment on VNPay

    par IPN callback (server-to-server)
        VNPay->>API: GET /payments/vnpay/ipn (vnp_ResponseCode, vnp_TxnRef, ...)
        API->>API: VnPayHelper.ValidateSignature(queryParams)
        alt Signature valid + ResponseCode=00
            API->>DB: Transaction.Status = Completed
            API->>DB: Order.MarkAsPaid()
            API->>DB: Create Escrow(status=Holding)
            API->>DB: WalletTransaction (record payment)
            API-->>VNPay: {"RspCode":"00","Message":"Confirm Success"}
        else Payment failed
            API->>DB: Transaction.Status = Failed
            API-->>VNPay: {"RspCode":"00","Message":"Confirm Success"}
        end
    and Return redirect (browser)
        VNPay->>Buyer: Redirect to returnUrl
        Buyer->>API: GET /payments/vnpay/return (vnp_ResponseCode, ...)
        API->>API: Validate signature, check status
        API-->>Buyer: Redirect to order confirmation page
    end
```

## Wallet Payment Sequence

```mermaid
sequenceDiagram
    autonumber
    participant Buyer
    participant API
    participant DB

    Buyer->>API: POST /payments/wallet/pay (orderId)
    API->>DB: Load Wallet, check balance >= totalAmount

    alt Sufficient balance
        API->>DB: Create Transaction(status=Processing, type=Payment)
        API->>DB: Wallet.Debit(amount) - reduce balance
        API->>DB: Create WalletTransaction(type=Debit)
        API->>DB: Transaction.Status = Completed
        API->>DB: Order.MarkAsPaid()
        API->>DB: Create Escrow(status=Holding, amount)
        API-->>Buyer: 200 {transactionId, status: "completed"}
    else Insufficient balance
        API-->>Buyer: 400 {error: "Insufficient wallet balance"}
    end
```

## Transaction Status State Machine

```mermaid
stateDiagram-v2
    [*] --> Pending: create transaction
    Pending --> Processing: begin processing
    Processing --> Completed: payment confirmed
    Processing --> Failed: payment failed
    Pending --> Cancelled: user cancels
    Completed --> Refunded: refund issued
    Failed --> [*]
    Cancelled --> [*]
    Refunded --> [*]
    Completed --> [*]
```

## Escrow Lifecycle

```mermaid
stateDiagram-v2
    [*] --> Holding: payment confirmed
    Holding --> ReleasedToSeller: order completed + decision window expired
    Holding --> RefundedToBuyer: dispute resolved in buyer favor
    Holding --> Disputed: dispute opened
    Disputed --> ReleasedToSeller: dispute resolved in seller favor
    Disputed --> RefundedToBuyer: dispute resolved in buyer favor
    ReleasedToSeller --> [*]
    RefundedToBuyer --> [*]
```

## Escrow Release Sequence

```mermaid
sequenceDiagram
    autonumber
    participant Job as ReleaseExpiredDecisionWindowJob
    participant DB
    participant Wallet as Seller Wallet

    Job->>DB: Find orders where Delivered + DecisionWindowEndsAt < now
    Job->>DB: Order.Complete()
    Job->>DB: Escrow.Status = ReleasedToSeller
    Job->>DB: Create EscrowReleaseEvent
    Job->>DB: Calculate platform fee
    Job->>Wallet: Credit seller wallet (amount - platformFee)
    Job->>DB: Create WalletTransaction for seller
```

## Withdrawal Status State Machine

```mermaid
stateDiagram-v2
    [*] --> Pending: seller requests withdrawal
    Pending --> Approved: admin approves
    Pending --> Rejected: admin rejects
    Pending --> Cancelled: seller cancels
    Approved --> Processing: begin bank transfer
    Processing --> Completed: transfer confirmed
    Completed --> [*]
    Rejected --> [*]
    Cancelled --> [*]
```
