# Hoan Tien VNPay (Refund)

## Tong quan

Admin co the hoan tien cho user qua VNPay Refund API. He thong tim Transaction goc, goi VNPay refund, va cap nhat trang thai Transaction thanh Refunded.

## Actors

- **Admin** - nguoi co quyen `ManagePayments`
- **System** - goi VNPay Refund API
- **VNPay** - xu ly hoan tien

## Endpoint Sequence

### Step 1: Yeu cau hoan tien
- **Method:** `POST /api/payments/vnpay/refund`
- **Auth:** Required (Permission: `ManagePayments`)
- **Request:**
  ```json
  {
    "originalTransactionRef": "20260319120000_abc123",
    "originalVnPayTransactionNo": "14232215",
    "amount": 500000,
    "reason": "Hoan tien do loi he thong"
  }
  ```
- **Response:** `200 OK`
  ```json
  {
    "isSuccess": true,
    "responseCode": "00",
    "message": "Refund thanh cong"
  }
  ```

## Business Logic Chi Tiet

### Buoc 1: Tim Transaction goc
- Tim Transaction theo `OriginalTransactionRef` (TransactionNumber)
- Neu khong tim thay -> loi `Transaction.NotFound`

### Buoc 2: Goi VNPay Refund API
- `IPaymentGatewayService.RefundAsync(refundRequest, ct)`
- Request chua:
  - `OriginalTransactionRef` - Ma giao dich goc
  - `OriginalVnPayTransactionNo` - Ma giao dich VNPay
  - `Amount` - So tien hoan
  - `Reason` - Ly do hoan
  - `IpAddress` - IP cua admin
  - `CreatedBy` - Username hoac UserId cua admin

### Buoc 3: Cap nhat Transaction
- Neu refund thanh cong: `transaction.MarkAsRefunded(now)`
- Luu thay doi vao DB

## Domain Events & Side Effects

- Transaction chuyen tu `Completed` -> `Refunded`
- Audit log duoc ghi nhan

## Luu y nghiep vu

- Chi Admin voi quyen `ManagePayments` moi co the hoan tien
- Hoan tien co the hoan mot phan hoac toan bo
- VNPay co the mat vai ngay de xu ly hoan tien ve tai khoan user
- Can co `OriginalVnPayTransactionNo` tu VNPay (khac voi TransactionRef cua he thong)
- IP address cua admin duoc gui kem de VNPay audit
