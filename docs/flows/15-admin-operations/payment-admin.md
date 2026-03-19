# Quan Ly Tai Chinh (Payment Admin)

## Tong quan

Admin co quyen xem tong hop tai chinh, quan ly giao dich, escrow, va duyet yeu cau rut tien. Bao gom ca chuc nang hoan tien qua VNPay.

## Actors

- **Admin** - nguoi co quyen `ReadPayments` (xem) va `ManagePayments` (xu ly)

## Endpoint Sequence

### Tong hop tai chinh
- **Method:** `GET /api/admin/payments/summary`
- **Auth:** Required (Permission: `ReadPayments`)
- **Query params:** `from`, `to` (khoang thoi gian)
- **Response:** `200 OK` -> `PaymentSummaryDto`
- **Ghi chu:** Tong hop doanh thu, so giao dich, so escrow, ...

### Quan ly giao dich (Transactions)

#### Danh sach giao dich
- **Method:** `GET /api/admin/payments/transactions`
- **Auth:** Required (Permission: `ReadPayments`)
- **Response:** `200 OK` -> Danh sach phan trang

#### Chi tiet giao dich
- **Method:** `GET /api/admin/payments/transactions/{transactionId}`
- **Auth:** Required (Permission: `ReadPayments`)
- **Response:** `200 OK`

### Quan ly Escrow

#### Danh sach escrow
- **Method:** `GET /api/admin/payments/escrows`
- **Auth:** Required (Permission: `ReadPayments`)
- **Response:** `200 OK` -> Danh sach phan trang

#### Chi tiet escrow
- **Method:** `GET /api/admin/payments/escrows/{escrowId}`
- **Auth:** Required (Permission: `ReadPayments`)
- **Response:** `200 OK`

### Quan ly rut tien (Withdrawals)

#### Danh sach yeu cau rut tien
- **Method:** `GET /api/admin/payments/withdrawals`
- **Auth:** Required (Permission: `ManagePayments`)
- **Response:** `200 OK` -> Danh sach phan trang

#### Chi tiet yeu cau
- **Method:** `GET /api/admin/payments/withdrawals/{withdrawalId}`
- **Auth:** Required (Permission: `ManagePayments`)
- **Response:** `200 OK`

#### Duyet rut tien
- **Method:** `POST /api/admin/payments/withdrawals/{withdrawalId}/approve`
- **Auth:** Required (Permission: `ManagePayments`)
- **Response:** `204 No Content`

#### Tu choi rut tien
- **Method:** `POST /api/admin/payments/withdrawals/{withdrawalId}/reject`
- **Auth:** Required (Permission: `ManagePayments`)
- **Request:**
  ```json
  {
    "reason": "Ly do tu choi"
  }
  ```
- **Response:** `204 No Content`

### Hoan tien VNPay
- **Method:** `POST /api/payments/vnpay/refund`
- **Auth:** Required (Permission: `ManagePayments`)
- **Request:**
  ```json
  {
    "originalTransactionRef": "20260319...",
    "originalVnPayTransactionNo": "14232215",
    "amount": 500000,
    "reason": "Hoan tien do loi he thong"
  }
  ```
- **Response:** `200 OK`
- Xem chi tiet tai [refund.md](../09-payment/refund.md)

## Domain Events & Side Effects

### Khi Approve Withdrawal:
- `WithdrawalApprovedDomainEvent` -> Audit log
- `WithdrawalCompletedDomainEvent` -> Notification: "Rut tien thanh cong"
- Tien duoc debit tu wallet

### Khi Reject Withdrawal:
- `WithdrawalRejectedDomainEvent` -> Audit log + Notification: "Yeu cau rut tien bi tu choi"
- Tien bi hold duoc unhold ve AvailableBalance

## Background Jobs

| Job | Interval | Mo ta |
|---|---|---|
| `GatewayReconciliationJob` | 15 phut | Doi soat giao dich Pending qua han |
| `ProcessGatewayWebhooksJob` | 10 giay | Xu ly webhook chua xu ly |

## Luu y nghiep vu

- ReadPayments chi cho phep xem, ManagePayments cho phep xu ly
- Withdrawal approval la thao tac thu cong - admin can verify thong tin ngan hang
- Hoan tien VNPay co the mat vai ngay de xu ly
- Payment summary giup admin nam bat tinh hinh tai chinh tong the
- Tat ca thao tac tai chinh deu duoc ghi audit log tu dong
