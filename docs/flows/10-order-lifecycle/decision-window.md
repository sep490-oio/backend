# Thoi Gian Quyet Dinh (Decision Window)

## Tong quan

Sau khi don hang duoc giao thanh cong, buyer co mot khoang thoi gian (decision window) de kiem tra hang va quyet dinh yeu cau tra hang hoac giu lai. Khi het thoi gian ma khong co yeu cau tra hang hay dispute, escrow duoc giai ngan cho seller tu dong.

## Actors

- **Buyer** - kiem tra hang va quyet dinh
- **System** (Background Job) - giai ngan khi het thoi gian
- **Seller** - nhan tien khi escrow duoc release

## Timeline

```
Giao hang -> Decision Window bat dau -> [N ngay] -> Decision Window ket thuc
                |                                          |
                | Buyer co the:                            | Neu khong co return/dispute:
                | - Yeu cau tra hang                       | -> Escrow release cho seller
                | - Mo dispute                             |
                | - Khong lam gi (auto-complete)            |
```

## Background Job: ReleaseExpiredDecisionWindowJob

- **Type:** `BackgroundService`
- **Interval:** 10 phut
- **Batch size:** 100 order/lan

### Dieu kien giai ngan:
```sql
WHERE Status = Delivered
  AND DisputedAt IS NULL
  AND DecisionWindowEndsAt IS NOT NULL
  AND DecisionWindowEndsAt <= NOW()
```

### Kiem tra them:
- Neu order co Return chua resolve (khong phai Rejected/Cancelled/Resolved): **bo qua**, cho return xu ly xong
- Neu Return da bi Rejected hoac khong co Return: giai ngan binh thuong

### Giai ngan:
- `EscrowSettlementService.ReleaseToSellerAsync(order, reason, actorId: null, ct)`
- Escrow chuyen tu `Holding` -> `ReleasedToSeller`
- Tien duoc credit vao wallet cua seller

## Domain Events & Side Effects

- `EscrowReleasedToSellerDomainEvent` -> Notification seller: "Tien giu da duoc giai ngan"
- `EscrowReleasedToSellerDomainEvent` -> Audit log

## Luu y nghiep vu

- Decision window duoc cau hinh qua `IRuntimeSettings.Order.ReturnDecisionWindowDays`
- Neu buyer mo dispute truoc khi het decision window: escrow bi dong bang cho den khi dispute resolve
- Neu buyer yeu cau tra hang: escrow bi dong bang cho den khi return resolve
- Sau khi decision window het va khong co van de: seller nhan tien tu dong
- Day la co che bao ve buyer - dam bao co thoi gian kiem tra hang
