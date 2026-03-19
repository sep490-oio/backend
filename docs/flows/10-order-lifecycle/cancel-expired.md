# Huy Don Hang Het Han Thanh Toan

## Tong quan

Background job `CancelExpiredOrdersJob` chay moi 5 phut de tim va huy cac don hang `PendingPayment` da qua `PaymentDueAt`. Khi huy, he thong danh dau auction la payment defaulted, tao risk flag cho buyer, va co the tu dong suspend user neu vi pham nhieu lan.

## Actors

- **System** (Background Job) - tu dong huy don hang
- **Buyer** - bi huy don hang

## Background Job

### CancelExpiredOrdersJob
- **Type:** `BackgroundService`
- **Interval:** 5 phut
- **Batch size:** 100 order/lan

## Business Logic Chi Tiet

### Buoc 1: Tim don hang het han
```sql
WHERE Status = PendingPayment
  AND PaymentDueAt IS NOT NULL
  AND PaymentDueAt < NOW()
LIMIT 100
```

### Buoc 2: Huy tung don hang
- `order.Cancel("Payment deadline expired", nowUtc)`
- Neu Cancel thanh cong: tiep tuc buoc 3
- Neu Cancel that bai: log warning, bo qua

### Buoc 3: Danh dau Auction payment defaulted
- `auction.MarkPaymentDefaulted(nowUtc)`
- Auction co the cho phep offer cho runner-up sau khi payment default

### Buoc 4: Tao Risk Flag cho Buyer
- `UserRiskFlag.Create(userId, "non_payment", reason, severity: Medium, ...)`
- Ghi nhan buyer vi pham khong thanh toan

### Buoc 5: Tao Monitoring Alert
- `MonitoringAlert.Create("User", buyerId, "repeated_non_payment", severity: Medium, ...)`
- Payload chua userId, orderId, auctionId

### Buoc 6: Tu dong Suspend user (neu cau hinh)
- Doc `runtimeSettings.Ops.AutoSuspendAfterNonPaymentCount`
- Neu so > 0: dem so risk flag "non_payment" cua user
- Neu so flag >= threshold: `user.ChangeStatus(UserStatus.Suspended, nowUtc)`

## Domain Events & Side Effects

- `OrderCancelledEvent` -> Notification "Don hang da bi huy"
- Risk flag duoc tao cho buyer
- Monitoring alert duoc tao cho admin review
- User co the bi suspend tu dong

## State Machine

```
Order: PendingPayment -> [Het han] -> Cancelled
Auction: Sold -> PaymentDefaulted (cho phep offer runner-up)
User: Active -> Suspended (neu vi pham nhieu lan)
```

## Luu y nghiep vu

- Payment deadline mac dinh la 48 gio tu khi auction ket thuc
- Moi lan payment default tao 1 risk flag "non_payment"
- Threshold tu dong suspend duoc cau hinh qua `IRuntimeSettings`
- Neu threshold = 0: khong tu dong suspend
- Buyer van co the bi suspend thu cong boi admin
- Sau khi payment default, seller co the offer cho runner-up
