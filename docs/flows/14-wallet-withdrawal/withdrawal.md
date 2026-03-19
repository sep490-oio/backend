# Rut Tien Ve Ngan Hang (Withdrawal)

## Tong quan

User co the rut tien tu wallet ve tai khoan ngan hang. Yeu cau rut tien can duoc admin duyet truoc khi tien duoc chuyen. User co the huy yeu cau truoc khi admin xu ly.

## Actors

- **User** - tao va quan ly yeu cau rut tien
- **Admin** - duyet hoac tu choi yeu cau

## Endpoint Sequence

### Step 1: Tao yeu cau rut tien
- **Method:** `POST /api/me/wallet/withdrawals`
- **Auth:** Required
- **Request:**
  ```json
  {
    "amount": 500000,
    "bankName": "Vietcombank",
    "accountNumber": "0123456789",
    "accountHolder": "NGUYEN VAN A"
  }
  ```
- **Response:** `200 OK` -> `CreateWithdrawalRequestResponse`
- **Error cases:**
  - `400 Bad Request` - So du khong du
  - `404 Not Found` - Wallet khong ton tai

### Step 2: Xem danh sach yeu cau
- **Method:** `GET /api/me/wallet/withdrawals`
- **Auth:** Required
- **Response:** `200 OK` -> `WithdrawalRequestDto[]`

### Step 3 (Optional): Huy yeu cau
- **Method:** `POST /api/me/wallet/withdrawals/{withdrawalId}/cancel`
- **Auth:** Required
- **Response:** `200 OK` -> `WithdrawalRequestDto`
- **Error cases:**
  - `409 Conflict` - Yeu cau da duoc xu ly, khong the huy

### Step 4: Admin duyet yeu cau
- **Method:** `POST /api/admin/payments/withdrawals/{withdrawalId}/approve`
- **Auth:** Required (Permission: `ManagePayments`)
- **Response:** `204 No Content`

### Step 4 (Alt): Admin tu choi yeu cau
- **Method:** `POST /api/admin/payments/withdrawals/{withdrawalId}/reject`
- **Auth:** Required (Permission: `ManagePayments`)
- **Response:** `204 No Content`

## Admin Xem Yeu Cau

### Danh sach tat ca yeu cau
- **Method:** `GET /api/admin/payments/withdrawals`
- **Auth:** Required (Permission: `ManagePayments`)

### Chi tiet yeu cau
- **Method:** `GET /api/admin/payments/withdrawals/{withdrawalId}`
- **Auth:** Required (Permission: `ManagePayments`)

## Withdrawal Status

```
Pending -> [Admin Approve] -> Approved -> Completed
       -> [Admin Reject]  -> Rejected (tien unhold)
       -> [User Cancel]   -> Cancelled (tien unhold)
```

## Domain Events & Side Effects

### Khi Approve:
- `WithdrawalApprovedDomainEvent` -> Audit log
- Tien duoc chuyen (process ngoai he thong)
- `WithdrawalCompletedDomainEvent` -> Notification: "Rut tien thanh cong"

### Khi Reject:
- `WithdrawalRejectedDomainEvent` -> Audit log + Notification: "Yeu cau rut tien bi tu choi"
- Tien bi hold duoc unhold ve AvailableBalance

### Khi Cancel:
- Tien bi hold duoc unhold ve AvailableBalance

## Luu y nghiep vu

- So tien rut khong vuot qua AvailableBalance
- Khi tao yeu cau: tien bi hold (chuyen tu Available sang Pending)
- Chi co the huy khi yeu cau con o trang thai Pending
- Admin can verify thong tin ngan hang truoc khi approve
- Viec chuyen tien thuc te la thao tac thu cong (ngoai he thong)
- Phi rut tien (neu co) duoc tinh khi approve
