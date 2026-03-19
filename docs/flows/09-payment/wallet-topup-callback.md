# Xu Ly Callback Nap Tien Vi (Wallet Top-Up)

## Tong quan

Khi user nap tien vao vi (WalletTopUp) thanh cong qua VNPay, he thong don gian credit so tien vao wallet cua user.

## Actors

- **User** - nguoi nap tien
- **System** - xu ly callback, credit wallet
- **VNPay** - gui ket qua thanh toan

## Endpoint Sequence

### Step 1: Tao URL nap tien
- **Method:** `POST /api/payments/vnpay/create-url`
- **Auth:** Required
- **Request:**
  ```json
  {
    "amount": 1000000,
    "currency": "VND",
    "purpose": "WalletTopUp",
    "description": "Nap tien vao vi OIO",
    "saveCard": true
  }
  ```
- **Response:** `200 OK` -> `{ transactionId, transactionRef, paymentUrl }`

### Step 2: User thanh toan tren VNPay

### Step 3: Callback xu ly tu dong

## Business Logic Chi Tiet (HandleWalletTopUpAsync)

### Buoc 1: Tim Wallet
- Tim wallet cua user theo `transaction.UserId`
- Neu khong tim thay -> loi `Wallet.NotFound`

### Buoc 2: Credit Wallet
- `wallet.Credit(amount, transactionId, description, nowUtc)`
- Tang `AvailableBalance` cua wallet
- Description: `"VNPay wallet top-up - TxnRef: {transactionRef}"`

### Buoc 3: Hoan thanh Transaction
- `transaction.MarkAsCompleted(gatewayInfo, nowUtc)`
- Auto-create PaymentMethod tu VNPay token (neu saveCard = true)

## Domain Events & Side Effects

- `TransactionCompletedDomainEvent` -> Audit log
- `WalletCreditedDomainEvent` -> Notification: "Vi du duoc cong tien. So du hien tai: {balance}"

## Luu y nghiep vu

- Day la flow don gian nhat trong cac callback
- Wallet top-up la mac dinh khi purpose khong phai AuctionDeposit/OrderPayment/AuctionBuyNow
- User co the nap tien bat ky luc nao
- So tien nap khong co gioi han tren (chi gioi han boi VNPay)
