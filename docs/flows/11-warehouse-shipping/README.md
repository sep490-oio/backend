# 11 - Warehouse & Shipping

## Tong quan

Module quan ly kho hang va van chuyen, bao gom: tiep nhan hang gui den (inbound), kiem tra chat luong (inspection), luu tru (storage), xuat hang (outbound), va theo doi van chuyen qua GHN.

## Thanh phan chinh

| Thanh phan | Mo ta |
|---|---|
| **InboundShipment** | Don hang gui den kho |
| **WarehouseItem** | San pham trong kho |
| **WarehouseInspection** | Ket qua kiem tra chat luong |
| **StorageLocation** | Vi tri luu tru trong kho |
| **OutboundShipment** | Don hang gui di tu kho |
| **ShippingProviderConfig** | Cau hinh nha van chuyen (GHN) |

## Cac subflow

| File | Mo ta |
|---|---|
| [inbound-shipment.md](./inbound-shipment.md) | Tiep nhan hang gui den |
| [inspection.md](./inspection.md) | Kiem tra chat luong |
| [inspector-workflow.md](./inspector-workflow.md) | **Flow end-to-end cua Inspector** (queue → inspect → review → store) |
| [storage.md](./storage.md) | Luu tru va quan ly vi tri kho |
| [outbound-shipment.md](./outbound-shipment.md) | Xuat hang gui di |
| [ghn-tracking.md](./ghn-tracking.md) | Theo doi van chuyen GHN |
| [cancel-shipment.md](./cancel-shipment.md) | Huy shipment |

## Permissions

| Permission | Mo ta |
|---|---|
| `warehouse:inbound:book` | Tao inbound shipment |
| `warehouse:outbound:book` | Tao outbound shipment |
| `warehouse:item:inspect` | Kiem tra hang va review inspection |
| `warehouse:item:store` | Luu tru hang trong kho |
| `warehouse:shipments:read` | Xem inspection queue va shipments |
| `warehouse:locations:manage` | Quan ly vi tri kho |

## Endpoints

| Method | URL | Mo ta |
|---|---|---|
| `POST` | `/api/warehouse/inbound-shipments` | Tao inbound shipment |
| `GET` | `/api/warehouse/inbound-shipments` | Danh sach inbound |
| `GET` | `/api/warehouse/inbound-shipments/{shipmentId}` | Chi tiet inbound |
| `POST` | `/api/warehouse/inbound-shipments/{shipmentId}/cancel` | Huy inbound |
| `POST` | `/api/warehouse/inbound-shipments/{shipmentId}/inspect` | Kiem tra hang |
| `POST` | `/api/warehouse/inbound-shipments/{shipmentId}/review` | Review ket qua kiem tra |
| `GET` | `/api/warehouse/inbound-shipments/inspection-queue` | Hang doi kiem tra |
| `POST` | `/api/warehouse/outbound-shipments` | Tao outbound shipment |
| `GET` | `/api/warehouse/outbound-shipments` | Danh sach outbound |
| `GET` | `/api/warehouse/outbound-shipments/{shipmentId}` | Chi tiet outbound |
| `POST` | `/api/warehouse/warehouse-items/{warehouseItemId}/store` | Luu tru hang |
| `GET` | `/api/warehouse/warehouse-items` | Danh sach hang trong kho |
| `POST` | `/api/warehouse/storage-locations` | Tao vi tri kho |
| `GET` | `/api/warehouse/storage-locations` | Danh sach vi tri kho |
| `PUT` | `/api/warehouse/storage-locations/{locationId}` | Cap nhat vi tri |
| `DELETE` | `/api/warehouse/storage-locations/{locationId}` | Xoa vi tri |
| `PUT` | `/api/warehouse/shipping-provider-configs/{configId}` | Cap nhat cau hinh van chuyen |
| `POST` | `/webhooks/ghn` | GHN webhook (anonymous) |

## Domain Events

- `InboundShipmentArrivedEvent` -> Tu dong vao hang doi inspection
- `OutboundShipmentPickedUpEvent` -> Order.MarkAsShipped + Notification
- `OutboundShipmentDeliveredEvent` -> Order.MarkAsDelivered + Notification
