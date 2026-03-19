# Giam Sat Canh Bao (Monitoring Alerts)

## Tong quan

He thong tu dong tao monitoring alerts khi phat hien cac tinh huong bat thuong: collusion (thong dong dau gia), non-payment, bid bashing, va cac mau dang ngo. Admin co the xem, xac nhan, va giai quyet cac alert.

## Actors

- **System** - tu dong tao alert tu background jobs va domain events
- **Admin** - nguoi co quyen `ReadItems` (xem) va `ManageItems` (xu ly)

## Endpoint Sequence

### Xem danh sach alerts
- **Method:** `GET /api/admin/monitoring-alerts`
- **Auth:** Required (Permission: `ReadItems`)
- **Query params:** `status`, `entityType`, `entityId`
- **Response:** `200 OK` -> `MonitoringAlertDto[]`

### Xac nhan nhan alert
- **Method:** `POST /api/admin/monitoring-alerts/{alertId}/acknowledge`
- **Auth:** Required (Permission: `ManageItems`)
- **Ghi chu:** Admin da nhan va dang xem xet

### Giai quyet alert
- **Method:** `POST /api/admin/monitoring-alerts/{alertId}/resolve`
- **Auth:** Required (Permission: `ManageItems`)
- **Ghi chu:** Admin da xu ly xong

## Nguon tao Alert

### Background Jobs
- **ScanActiveAuctionsForCollusionJob**: Quet dau gia dang hoat dong de phat hien thong dong
  - Pattern: nhieu bid tu cung IP, cung thoi gian, hoac tu tai khoan lien ket
  - Severity: High

### Domain Events
- **CancelExpiredOrdersJob**: Tao alert `repeated_non_payment` khi buyer khong thanh toan
  - Entity: User
  - Severity: Medium

### Admin Manual
- **FlagAuction**: Admin tu tao alert cho auction
  - `POST /api/admin/auctions/{auctionId}/alerts`
  - Request: `{ alertType, severity, payload }`

## Alert Status

```
New -> Acknowledged -> Resolved
```

## Alert Severity

| Severity | Mo ta |
|---|---|
| `Low` | Thong tin, khong can hanh dong ngay |
| `Medium` | Can xem xet |
| `High` | Can xu ly som |
| `Critical` | Can xu ly ngay lap tuc |

## Luu y nghiep vu

- Monitoring alerts la co che canh bao som, khong tu dong xu ly
- Admin can xem xet va quyet dinh hanh dong (ban user, huy auction, ...)
- Alert co the lien ket voi entity cu the (User, Auction, ...)
- Payload JSON chua du lieu chi tiet de admin dieu tra
- He thong khong tu dong hanh dong dua tren alert (ngoai tru auto-suspend non-payment)
