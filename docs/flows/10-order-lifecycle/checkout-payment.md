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

## Business Logic Chi Tiet

### Buoc 1: Lay Order
- Tim Order theo `orderId`
- Neu khong tim thay -> `Order.NotFound`

### Buoc 2: Initialize Payment
- `order.InitializePayment(now)`
- Danh dau order bat dau quy trinh thanh toan
- Luu thay doi

### Buoc 3: Delegate tao URL
- Goi `CreateVnPayPaymentUrlCommand` voi:
  - `Amount = order.Pricing.TotalAmount.Amount`
  - `Currency = order.Currency`
  - `Purpose = OrderPayment`
  - `Description = "OrderPayment - Order #{orderNumber}"`
  - `AuctionId = order.AuctionId`
  - `OrderId = order.Id`

### Buoc 4: Tra ve URL cho frontend
- Frontend redirect user sang VNPay
- Sau khi thanh toan, VNPay goi IPN va redirect user ve Return URL

## Lien ket voi Payment Flow

- Sau khi user thanh toan thanh cong tren VNPay:
  - IPN callback -> `ProcessVnPayCallbackCommand` -> `HandleOrderPaymentAsync`
  - Tao Escrow, cap nhat Order -> `Paid`
  - Xem chi tiet tai [order-payment-callback.md](../09-payment/order-payment-callback.md)

## Luu y nghiep vu

- Checkout la wrapper endpoint, don gian hoa flow cho frontend
- Frontend chi can truyen `orderId`, khong can biet purpose/amount
- BankCode la tuy chon (neu khong truyen, VNPay se hien danh sach ngan hang)
- Neu order da qua PaymentDueAt, order se bi huy boi background job truoc khi user co the checkout
