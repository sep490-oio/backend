# Huy Shipment

## Tong quan

Nhan vien kho co the huy inbound shipment khi can thiet (vi du: seller huy gui hang, thong tin sai, ...).

## Actors

- **Warehouse Staff** - nguoi co quyen `Warehouse.ReadShipments`

## Endpoint Sequence

### Huy Inbound Shipment
- **Method:** `POST /api/warehouse/inbound-shipments/{shipmentId}/cancel`
- **Auth:** Required (Permission: `Warehouse.ReadShipments`)
- **Request:**
  ```json
  {
    "reason": "Ly do huy"
  }
  ```
- **Response:** `204 No Content`
- **Error cases:**
  - `404 Not Found` - Shipment khong ton tai
  - `409 Conflict` - Shipment o trang thai khong the huy

## Business Logic

- Chi co the huy shipment o trang thai cho xu ly
- Shipment da nhan hoac da kiem tra khong the huy
- Ly do huy duoc ghi lai de audit

## Luu y nghiep vu

- Huy inbound shipment khong anh huong den item/auction
- Seller co the tao shipment moi sau khi huy
- Can ghi ro ly do de co co so doi soat
