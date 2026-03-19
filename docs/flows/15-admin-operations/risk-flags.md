# Quan Ly Co Rui Ro (Risk Flags)

## Tong quan

Risk flags danh dau nguoi dung co hanh vi dang ngo hoac vi pham. Flags co the duoc tao tu dong (boi system) hoac thu cong (boi admin). Tich luy nhieu flags co the dan den tu dong suspend user.

## Actors

- **Admin** - nguoi co quyen `ManageUsers`
- **System** - tu dong tao flag

## Endpoint

### Tao Risk Flag cho User
- **Method:** `POST /api/admin/users/{userId}/risk-flags`
- **Auth:** Required (Permission: `ManageUsers`)
- **Request:**
  ```json
  {
    "flagType": "non_payment | fraud | collusion | harassment | suspicious_activity",
    "reason": "Mo ta ly do",
    "severity": "low | medium | high | critical"
  }
  ```
- **Response:** `200 OK` -> `UserRiskFlagDto`

## Nguon tao Risk Flag tu dong

### CancelExpiredOrdersJob
- Khi order het han thanh toan:
  - `flagType = "non_payment"`
  - `severity = Medium`
  - `reason = "Order {orderNumber} expired without payment"`

### ScanActiveAuctionsForCollusionJob
- Khi phat hien mau thong dong:
  - `flagType = "collusion"`
  - `severity = High`

## Tu dong Suspend

- Cau hinh: `IRuntimeSettings.Ops.AutoSuspendAfterNonPaymentCount`
- Logic: Khi so risk flag "non_payment" >= threshold -> tu dong suspend user
- Chi ap dung cho flag type "non_payment"
- Threshold = 0 nghia la tat chuc nang tu dong suspend

## Risk Flag Types

| Type | Mo ta | Tu dong? |
|---|---|---|
| `non_payment` | Khong thanh toan don hang | Co (CancelExpiredOrdersJob) |
| `fraud` | Lua dao | Khong (admin thu cong) |
| `collusion` | Thong dong dau gia | Co (CollusionScanJob) |
| `harassment` | Quay roi | Khong (admin thu cong) |
| `suspicious_activity` | Hoat dong dang ngo | Khong (admin thu cong) |

## Luu y nghiep vu

- Risk flags khong tu dong xoa - chi co the review boi admin
- Nhieu flags cung type tang severity cua user
- Admin co the dung risk flags de quyet dinh ban/suspend user
- Tu dong suspend chi xay ra voi non_payment, cac type khac can admin xu ly thu cong
- Risk flags duoc ghi audit log
