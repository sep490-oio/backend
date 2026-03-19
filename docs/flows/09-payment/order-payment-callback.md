# Xu Ly Callback Thanh Toan Don Hang

## Tong quan

Khi user thanh toan don hang (OrderPayment) thanh cong qua VNPay, he thong tao Escrow de giu tien, chuyen deposit dau gia (neu co) thanh payment, va danh dau Order la da thanh toan.

## Actors

- **Buyer** - nguoi thanh toan don hang
- **System** - xu ly callback, tao escrow, cap nhat order
- **VNPay** - gui ket qua thanh toan

## Endpoint Sequence

### Step 1: Checkout Order (tao URL thanh toan)
- **Method:** `POST /api/payments/checkout`
- **Auth:** Required
- **Request:**
  ```json
  {
    "orderId": "guid",
    "bankCode": "VNBANK"
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
- **Ghi chu:** Endpoint nay la wrapper, noi bo goi `CreateVnPayPaymentUrlCommand` voi `Purpose = OrderPayment`

### Step 2: Order.InitializePayment
- Truoc khi tao URL, he thong goi `order.InitializePayment(now)` de chuyen trang thai

### Step 3: VNPay Callback (xu ly tu dong)

## Business Logic Chi Tiet (HandleOrderPaymentAsync)

### Buoc 1: Validate
- Transaction phai co `OrderId`
- Order phai ton tai

### Buoc 2: Tao Escrow
- `Escrow.Create(orderId, transactionId, amount, currency, nowUtc)`
- Escrow giu tien cho den khi don hang giao thanh cong

### Buoc 3: Xu ly Winner Deposit (neu co)
- Tim `AuctionDeposit` cua winner dang `IsHeld`
- Neu co:
  - `winnerDeposit.ConvertToPayment(nowUtc)` - chuyen deposit thanh payment
  - `wallet.DebitPending(depositAmount)` - tru tien tu pending balance
  - Deposit duoc ap dung nhu mot phan thanh toan

### Buoc 4: Danh dau Order da thanh toan
- `order.MarkAsPaid(nowUtc)`
- Order chuyen sang trang thai `Paid`

### Buoc 5: Hoan thanh Transaction
- `transaction.MarkAsCompleted(gatewayInfo, nowUtc)`

## State Machine

```
Order: PendingPayment -> InitializePayment -> [VNPay] -> Paid
Escrow: -> Created (Holding)
Deposit: Held -> ConvertedToPayment (neu co)
Transaction: Pending -> Completed
```

## Domain Events & Side Effects

- `TransactionCompletedDomainEvent` -> Audit log
- `WalletCreditedDomainEvent` (neu co wallet credit)
- Order chuyen sang `Paid` -> trigger shipping flow
- Escrow duoc tao de bao ve ca buyer va seller

## Luu y nghiep vu

- Checkout endpoint la cach chinh de thanh toan don hang (thay vi goi truc tiep create-url)
- So tien thanh toan = `order.Pricing.TotalAmount`
- Neu user da dat coc dau gia, deposit duoc tru vao tong tien can thanh toan
- Escrow chi duoc release cho seller sau khi don hang giao thanh cong va het thoi gian quyet dinh
