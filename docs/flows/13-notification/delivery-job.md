# Background Job Gui Thong Bao (Delivery Job)

## Tong quan

`ProcessNotificationDeliveriesJob` la Quartz job xu ly viec gui thong bao qua cac kenh. Job lay cac delivery dang Pending hoac Failed (can retry), tim provider tuong ung, va gui thong bao.

## Background Job: ProcessNotificationDeliveriesJob

- **Scheduler:** Quartz.NET
- **Concurrency:** `[DisallowConcurrentExecution]`
- **Batch size:** 50 delivery/lan

## Business Logic Chi Tiet

### Buoc 1: Lay Pending Deliveries
```sql
WHERE (Status = Pending AND ScheduledAt <= NOW())
   OR (Status = Failed AND NextRetryAt <= NOW() AND AttemptCount < MaxAttempts)
ORDER BY ScheduledAt
LIMIT 50
```

### Buoc 2: Tim Provider
- Voi moi delivery, tim `INotificationProvider` khop voi `delivery.Channel`
- Neu khong tim thay provider: `delivery.MarkAsFailed("NO_PROVIDER", ...)`

### Buoc 3: Gui thong bao
- `provider.SendAsync(notification, delivery, ct)`
- Neu thanh cong: `delivery.MarkAsSent(now)`
- Neu that bai: `delivery.MarkAsFailed("SEND_ERROR", result.Error, now)`

### Buoc 4: Xu ly exception
- Neu exception: `delivery.MarkAsFailed("EXCEPTION", ex.Message, now)`
- Khong throw exception ra ngoai de tiep tuc xu ly delivery khac

### Buoc 5: Luu thay doi
- `SaveChangesAsync()` sau khi xu ly tat ca delivery trong batch

## Retry Logic

- `MarkAsFailed` tu dong tinh `NextRetryAt` va tang `AttemptCount`
- Retry chi xay ra khi `AttemptCount < MaxAttempts`
- Sau MaxAttempts: delivery o trang thai `Failed` vinh vien

## NotificationDelivery Status

```
Pending -> [Gui thanh cong] -> Sent
       -> [Gui that bai] -> Failed -> [Retry] -> Pending -> ...
       -> [Het retry] -> Failed (final)
```

## Luu y nghiep vu

- Job chay voi `[DisallowConcurrentExecution]` de tranh gui trung
- Moi notification co the co nhieu delivery (1 per kenh)
- Failed delivery duoc retry tu dong theo cau hinh
- Exception trong 1 delivery khong anh huong cac delivery khac
