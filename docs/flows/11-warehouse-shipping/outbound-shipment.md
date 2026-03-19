# Xuat Hang Gui Di (Outbound Shipment)

## Tong quan

Khi don hang da thanh toan, nhan vien kho tao outbound shipment de gui hang cho buyer qua don vi van chuyen (GHN). He thong tu dong cap nhat trang thai order khi shipment duoc pick up va delivered.

## Actors

- **Warehouse Staff** - nguoi co quyen `Warehouse.BookOutbound`
- **GHN** - don vi van chuyen

## Endpoint Sequence

### Step 1: Tao Outbound Shipment
- **Method:** `POST /api/warehouse/outbound-shipments`
- **Auth:** Required (Permission: `Warehouse.BookOutbound`)
- **Request:** `BookOutboundShipmentCommand` (body)
- **Response:** `201 Created`
- **Ghi chu:** Tao don van chuyen tren GHN API

### Step 2: Xem danh sach Outbound
- **Method:** `GET /api/warehouse/outbound-shipments`
- **Auth:** Required
- **Response:** `200 OK`

### Step 3: Xem chi tiet
- **Method:** `GET /api/warehouse/outbound-shipments/{shipmentId}`
- **Auth:** Required
- **Response:** `200 OK`

## Domain Events & Side Effects

### Khi GHN pick up hang:
- `OutboundShipmentPickedUpEvent`:
  - `OrderMarkedShippedEventHandler` -> `order.MarkAsShipped()`
  - `OrderShippedNotificationHandler` -> Thong bao buyer

### Khi GHN giao hang thanh cong:
- `OutboundShipmentDeliveredEvent`:
  - `OrderMarkedDeliveredEventHandler` -> `order.MarkAsDelivered()` + Decision Window
  - `OrderDeliveredNotificationHandler` -> Thong bao buyer (2 notification: giao hang + decision window)

## State Machine

```
OutboundShipment: Created -> PickedUp -> InTransit -> Delivered
                                                   -> Failed
```

## Luu y nghiep vu

- Outbound shipment chi tao duoc khi order da thanh toan (Paid)
- He thong goi GHN API de tao don van chuyen tu dong
- Trang thai shipment duoc cap nhat qua GHN webhook
- `ClientOrderCode` chua AuctionId de lien ket voi auction
