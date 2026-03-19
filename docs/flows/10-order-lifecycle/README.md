# 10 - Order Lifecycle

## Tong quan

Module quan ly vong doi don hang tu khi duoc tao tu dong sau dau gia/buy-now, qua thanh toan, van chuyen, giao hang, thoi gian quyet dinh (decision window), tra hang, cho den khi hoan tat.

## Order Status (State Machine)

```
PendingPayment -> Paid -> Shipped -> Delivered -> Completed
                                                -> ReturnRequested -> ...
PendingPayment -> Cancelled (het han thanh toan)
```

| Status | Mo ta |
|---|---|
| `PendingPayment` | Cho thanh toan (co `PaymentDueAt`) |
| `Paid` | Da thanh toan, cho van chuyen |
| `Shipped` | Da giao cho don vi van chuyen |
| `Delivered` | Da giao thanh cong, bat dau decision window |
| `Completed` | Hoan tat (sau decision window hoac auto-complete) |
| `Cancelled` | Da huy (het han thanh toan) |
| `ReturnRequested` | Buyer yeu cau tra hang |

## Cac subflow

| File | Mo ta |
|---|---|
| [auto-create-from-auction.md](./auto-create-from-auction.md) | Tu dong tao order khi dau gia ket thuc |
| [checkout-payment.md](./checkout-payment.md) | Thanh toan don hang |
| [cancel-expired.md](./cancel-expired.md) | Huy don hang het han thanh toan |
| [payment-default.md](./payment-default.md) | Xu ly khi buyer khong thanh toan |
| [shipped-delivered.md](./shipped-delivered.md) | Cap nhat trang thai van chuyen/giao hang |
| [decision-window.md](./decision-window.md) | Thoi gian quyet dinh sau giao hang |
| [return-flow.md](./return-flow.md) | Luong tra hang |
| [auto-complete.md](./auto-complete.md) | Tu dong hoan tat don hang |

## Background Jobs

| Job | Interval | Mo ta |
|---|---|---|
| `CancelExpiredOrdersJob` | 5 phut | Huy don hang het han thanh toan |
| `ReleaseExpiredDecisionWindowJob` | 10 phut | Giai ngan escrow khi het decision window |
| `AuctionAutoCompleteJob` | Quartz (cau hinh) | Tu dong hoan tat auction sau giao hang + N ngay |

## Domain Events

- `AuctionSoldEvent` -> Tao Order tu dong
- `OutboundShipmentPickedUpEvent` -> Order.MarkAsShipped
- `OutboundShipmentDeliveredEvent` -> Order.MarkAsDelivered
- `OrderCancelledEvent` -> Notification buyer

## Endpoints

| Method | URL | Mo ta |
|---|---|---|
| `GET` | `/api/orders/{orderId}` | Xem chi tiet don hang |
| `GET` | `/api/me/orders` | Danh sach don hang cua toi |
| `POST` | `/api/payments/checkout` | Thanh toan don hang |
| `POST` | `/api/orders/{orderId}/returns` | Yeu cau tra hang |
| `POST` | `/api/orders/{orderId}/returns/{returnId}/approve` | Duyet tra hang |
| `POST` | `/api/orders/{orderId}/returns/{returnId}/reject` | Tu choi tra hang |
| `POST` | `/api/orders/{orderId}/returns/{returnId}/ship` | Gui hang tra |
| `POST` | `/api/orders/{orderId}/returns/{returnId}/confirm-received` | Xac nhan nhan hang tra |
