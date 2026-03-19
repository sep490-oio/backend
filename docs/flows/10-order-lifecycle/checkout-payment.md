# Thanh Toan Don Hang (Checkout)

## Tong quan

Buyer su dung endpoint Checkout de thanh toan don hang. He thong chuyen trang thai order sang `InitializePayment`, tao VNPay URL, va redirect user de thanh toan.

## Actors

- **Buyer** - nguoi thanh toan
- **System** - tao URL thanh toan

## Endpoint Sequence

### Step 1: Checkout Order
- **Method:** `POST /api/payments/checkout`
- **Auth:** Required
- **Request:**
  ```json
  {
    "orderId": "guid",
    "bankCode": "VNBANK",
    "paymentMethod": "vnpay | wallet | wallet_vnpay"
  }
  ```
- **Response:** `200 OK`
  ```json
  {
    "transactionId": "guid",
    "transactionRef": "20260319...",
    "paymentUrl": "https://sandbox.vnpayment.vn/... (null neu wallet)"
  }
  ```

## Business Logic Chi Tiet

### Buoc 1: Lay Order
- Tim Order theo `orderId`
- Neu khong tim thay -> `Order.NotFound`

### Buoc 2: Initialize Payment
- `order.InitializePayment(now)`
- Danh dau order bat dau quy trinh thanh toan
- Luu thay doi

### Buoc 3: Phan nhanh theo PaymentMethod

He thong ho tro 3 che do checkout:

| PaymentMethod | Mo ta |
|---|---|
| `vnpay` | **(Mac dinh)** Tao VNPay URL, redirect user de thanh toan |
| `wallet` | Tru tien truc tiep tu Wallet, tao Escrow, mark order Paid ngay lap tuc |
| `wallet_vnpay` | Hybrid: hold wallet balance + tao VNPay URL cho phan con lai |

**Flow `vnpay` (mac dinh):**
- Goi `CreateVnPayPaymentUrlCommand` voi:
  - `Amount = order.Pricing.TotalAmount.Amount`
  - `Currency = order.Currency`
  - `Purpose = OrderPayment`
  - `Description = "OrderPayment - Order #{orderNumber}"`
  - `AuctionId = order.AuctionId`
  - `OrderId = order.Id`

**Flow `wallet`:**
- Lay Wallet cua buyer, tinh `remainingAmount` (sau khi tru deposit)
- Kiem tra so du du -> tru Wallet -> tao Escrow -> mark order Paid
- Response `paymentUrl = null`

**Flow `wallet_vnpay`:**
- Tinh `walletPortion` va `vnpayPortion`
- Neu wallet du tien -> fallback ve flow `wallet`
- Neu khong: `wallet.Hold(walletPortion)` + tao VNPay URL cho `vnpayPortion`
- Neu tao URL loi: rollback hold

### Buoc 4: Tra ve ket qua cho frontend
- Voi `vnpay` / `wallet_vnpay`: frontend redirect user sang VNPay
- Voi `wallet`: khong can redirect, don hang da duoc thanh toan
- Sau khi thanh toan VNPay, VNPay goi IPN va redirect user ve Return URL

## Lien ket voi Payment Flow

- Sau khi user thanh toan thanh cong tren VNPay:
  - IPN callback -> `ProcessVnPayCallbackCommand` -> `HandleOrderPaymentAsync`
  - Tao Escrow, cap nhat Order -> `Paid`
  - Xem chi tiet tai [order-payment-callback.md](../09-payment/order-payment-callback.md)

## Luu y nghiep vu

- Checkout la wrapper endpoint, don gian hoa flow cho frontend
- Frontend chi can truyen `orderId`, khong can biet purpose/amount
- `paymentMethod` mac dinh la `"vnpay"` neu khong truyen
- BankCode la tuy chon (neu khong truyen, VNPay se hien danh sach ngan hang), chi ap dung cho `vnpay` va `wallet_vnpay`
- Neu order da qua PaymentDueAt, order se bi huy boi background job truoc khi user co the checkout
- Voi `wallet`: buyer can co du so du trong Wallet, neu khong se tra loi `Wallet.InsufficientBalance`
- Voi `wallet_vnpay`: deposit auction winner duoc tru truoc, sau do wallet, cuoi cung VNPay cho phan con lai
