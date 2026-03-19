# Tu Dong Hoan Tat Don Hang (Auto-Complete)

## Tong quan

Background job `AuctionAutoCompleteJob` tu dong hoan tat cac auction da ban khi don hang da giao thanh cong va khong co dispute/return trong thoi gian quy dinh.

## Actors

- **System** (Background Job) - tu dong hoan tat

## Background Job: AuctionAutoCompleteJob

- **Scheduler:** Quartz.NET
- **Concurrency:** `[DisallowConcurrentExecution]`
- **Dieu kien:** Cau hinh `App.Constraint.Auction.AutoCompleteDaysAfterDelivery`

## Business Logic Chi Tiet

### Buoc 1: Tim auction da ban
```sql
WHERE Auction.Status = Sold
```

### Buoc 2: Kiem tra tung auction
Voi moi auction, kiem tra:

1. **Co outbound shipment da giao?**
   - Tim `OutboundShipment` chua `AuctionId` trong `ClientOrderCode`
   - `Status = Delivered`
   - `DeliveredAt` phai truoc cutoff (`now - AutoCompleteDays`)

2. **Khong co dispute mo?**
   - Kiem tra `Dispute` voi `AuctionId`
   - Khong co dispute nao o trang thai khac `Resolved`, `Closed`, `Cancelled`

### Buoc 3: Hoan tat
- Neu ca 2 dieu kien thoa man: `auction.Item.MarkSold(now)`
- Luu thay doi

## State Machine

```
Auction: Sold -> [N ngay sau delivery, khong dispute] -> Auto-completed (Item.MarkSold)
```

## Moi quan he voi Decision Window

- `ReleaseExpiredDecisionWindowJob` giai ngan escrow (tien)
- `AuctionAutoCompleteJob` danh dau item la da ban (trang thai)
- Hai job hoat dong doc lap nhung bo sung cho nhau

## Luu y nghiep vu

- `AutoCompleteDaysAfterDelivery` la so ngay tinh tu `DeliveredAt`
- Neu co dispute dang mo: khong auto-complete, cho dispute resolve
- Job chi xu ly auction o trang thai `Sold` (da co nguoi thang va thanh toan)
- Auto-complete danh dau item la "da ban" - khong the relist lai
