# Nap Tien Vao Vi (Wallet Top-Up)

## Tong quan

User nap tien vao vi qua VNPay. Tien duoc credit vao wallet sau khi VNPay callback thanh cong.

## Actors

- **User** - nguoi nap tien
- **VNPay** - cong thanh toan

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
- **Response:** `200 OK`
  ```json
  {
    "transactionId": "guid",
    "transactionRef": "20260319...",
    "paymentUrl": "https://sandbox.vnpayment.vn/..."
  }
  ```

### Step 2: User thanh toan tren VNPay
- Frontend redirect user sang `paymentUrl`
- User nhap thong tin the va xac nhan

### Step 3: VNPay callback
- Xem chi tiet tai [wallet-topup-callback.md](../09-payment/wallet-topup-callback.md)
- `wallet.Credit(amount, transactionId, description, now)`

## Xem so du vi
- **Method:** `GET /api/me/wallet`
- **Auth:** Required
- **Response:** `200 OK` -> `WalletSummaryDto`
  - `AvailableBalance` - So du kha dung
  - `PendingBalance` - So du tam giu
  - `TotalBalance` - Tong so du

## Domain Events

- `WalletCreditedDomainEvent` -> Notification: "Vi du duoc cong {amount}. So du hien tai: {balance}"

## Luu y nghiep vu

- Tien nap vao wallet la AvailableBalance, co the dung ngay
- `saveCard = true` se luu token the de nap nhanh lan sau
- So tien nap toi thieu/toi da tuy cau hinh VNPay
- Wallet duoc tao tu dong khi user dang ky (UserCreatedEventHandler)
