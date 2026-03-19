# Cap Nhat Trang Thai Van Chuyen & Giao Hang

## Tong quan

Trang thai van chuyen cua order duoc cap nhat tu dong qua domain events tu Warehouse context. Khi outbound shipment duoc pick up, order chuyen sang `Shipped`. Khi shipment delivered, order chuyen sang `Delivered` va bat dau decision window.

## Actors

- **System** - tu dong cap nhat qua domain events
- **GHN** (don vi van chuyen) - gui webhook cap nhat trang thai
- **Buyer** - nhan thong bao

## Domain Event Handlers

### OrderMarkedShippedEventHandler
- **Trigger:** `OutboundShipmentPickedUpEvent`
- **Logic:**
  1. Tim Order theo `OrderId` tu event
  2. `order.MarkAsShipped(clock.UtcNow)`
  3. Luu thay doi
- **Ket qua:** Order chuyen tu `Paid` -> `Shipped`

### OrderMarkedDeliveredEventHandler
- **Trigger:** `OutboundShipmentDeliveredEvent`
- **Logic:**
  1. Tim Order theo `OrderId` tu event
  2. Doc `runtimeSettings.Order.ReturnDecisionWindowDays` (so ngay decision window)
  3. `order.MarkAsDelivered(deliveredAt, decisionWindowEndsAt, nowUtc)`
  4. Luu thay doi
- **Ket qua:** Order chuyen tu `Shipped` -> `Delivered`
- **Decision Window:** `DecisionWindowEndsAt = DeliveredAt + ReturnDecisionWindowDays`

## Notification Handlers

### OrderShippedNotificationHandler
- **Trigger:** `OutboundShipmentPickedUpEvent`
- **Recipient:** Buyer
- **Type:** `order_shipped`
- **Message:** "Don hang {orderNumber} da duoc ban giao cho don vi van chuyen. Ma van don: {trackingNumber}"

### OrderDeliveredNotificationHandler
- **Trigger:** `OutboundShipmentDeliveredEvent`
- **Recipient:** Buyer
- **Type:** `order_delivered`
- **Message:** "Don hang {orderNumber} da duoc giao thanh cong vao {deliveredAt}"
- **Notification 2:** `decision_window_started`
- **Message:** "Ban co the yeu cau tra hang cho don {orderNumber} truoc {decisionWindowEndsAt}"

## State Machine

```
Order: Paid -> [OutboundShipmentPickedUp] -> Shipped -> [OutboundShipmentDelivered] -> Delivered
```

## Luu y nghiep vu

- Trang thai order duoc cap nhat tu dong, khong co endpoint thu cong
- GHN webhook -> Warehouse context -> Domain events -> Order context
- Decision window cho phep buyer kiem tra hang va yeu cau tra hang neu can
- So ngay decision window duoc cau hinh qua `IRuntimeSettings.Order.ReturnDecisionWindowDays`
