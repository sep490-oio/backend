# Tiep Nhan Hang Gui Den (Inbound Shipment)

## Tong quan

Khi seller gui san pham den kho OIO de kiem tra truoc khi dau gia, nhan vien kho tao inbound shipment de theo doi. Hang khi den kho se tu dong vao hang doi kiem tra (inspection queue).

## Actors

- **Warehouse Staff** - nguoi co quyen `Warehouse.BookInbound`
- **Seller** - gui hang den kho

## Endpoint Sequence

### Step 1: Tao Inbound Shipment
- **Method:** `POST /api/warehouse/inbound-shipments`
- **Auth:** Required (Permission: `Warehouse.BookInbound`)
- **Request:** `BookInboundShipmentCommand` (body)
- **Response:** `201 Created`

### Step 2: Xem danh sach Inbound
- **Method:** `GET /api/warehouse/inbound-shipments`
- **Auth:** Required (Permission: `Warehouse.ReadShipments`)
- **Response:** `200 OK` -> Danh sach phan trang

### Step 3: Xem chi tiet
- **Method:** `GET /api/warehouse/inbound-shipments/{shipmentId}`
- **Auth:** Required (Permission: `Warehouse.ReadShipments`)
- **Response:** `200 OK`

## Domain Events & Side Effects

- `InboundShipmentArrivedEvent` -> Hang tu dong vao inspection queue
- Nhan vien kho co the bat dau kiem tra

## Luu y nghiep vu

- Inbound shipment theo doi quy trinh tu luc hang gui den cho den khi nhan va kiem tra
- Mot inbound shipment lien ket voi 1 item/auction
- Sau khi kiem tra xong, hang duoc luu tru trong kho
