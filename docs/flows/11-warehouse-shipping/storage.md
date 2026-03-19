# Luu Tru & Quan Ly Vi Tri Kho (Storage)

## Tong quan

Sau khi hang duoc kiem tra va chap nhan, nhan vien kho gan vi tri luu tru (storage location) cho hang. He thong ho tro tao, cap nhat, xoa vi tri kho.

## Actors

- **Warehouse Staff** - nguoi co quyen `Warehouse.Store`

## Endpoint Sequence

### Luu tru hang vao vi tri
- **Method:** `POST /api/warehouse/warehouse-items/{warehouseItemId}/store`
- **Auth:** Required (Permission: `Warehouse.Store`)
- **Request:**
  ```json
  {
    "storageLocationId": "guid"
  }
  ```
- **Response:** `200 OK` -> `WarehouseItemDto`

### Xem danh sach hang trong kho
- **Method:** `GET /api/warehouse/warehouse-items`
- **Auth:** Required
- **Response:** `200 OK`

## Quan ly vi tri kho

### Tao vi tri kho
- **Method:** `POST /api/warehouse/storage-locations`
- **Auth:** Required
- **Response:** `201 Created`

### Xem danh sach vi tri
- **Method:** `GET /api/warehouse/storage-locations`
- **Auth:** Required
- **Response:** `200 OK`

### Cap nhat vi tri
- **Method:** `PUT /api/warehouse/storage-locations/{locationId}`
- **Auth:** Required
- **Response:** `200 OK`

### Xoa vi tri
- **Method:** `DELETE /api/warehouse/storage-locations/{locationId}`
- **Auth:** Required
- **Response:** `204 No Content`
- **Ghi chu:** Chi xoa duoc khi vi tri khong co hang

## Luu y nghiep vu

- Moi warehouse item chi o 1 vi tri tai mot thoi diem
- Vi tri kho co ten va mo ta de de tim kiem
- Khong the xoa vi tri dang co hang
- Storage location giup nhan vien kho tim hang nhanh khi can xuat
