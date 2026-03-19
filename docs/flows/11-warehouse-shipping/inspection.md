# Kiem Tra Chat Luong (Inspection)

## Tong quan

Nhan vien kho kiem tra tinh trang san pham khi nhan hang tu seller. Ket qua kiem tra duoc review va quyet dinh Accept/Reject. Neu condition khac voi mo ta cua seller, he thong yeu cau seller xac nhan.

## Actors

- **Warehouse Inspector** - nguoi co quyen `Warehouse.Inspect`
- **Warehouse Reviewer** - nguoi review ket qua kiem tra
- **Seller** - xac nhan condition moi (neu khac voi mo ta)

## Endpoint Sequence

### Step 1: Xem hang doi kiem tra
- **Method:** `GET /api/warehouse/inbound-shipments/inspection-queue`
- **Auth:** Required (Permission: `Warehouse.Inspect`)
- **Response:** `200 OK`

### Step 2: Kiem tra hang
- **Method:** `POST /api/warehouse/inbound-shipments/{shipmentId}/inspect`
- **Auth:** Required (Permission: `Warehouse.Inspect`)
- **Request:**
  ```json
  {
    "conditionId": "like_new | good | fair | poor",
    "inspectionNotes": "Ghi chu kiem tra",
    "inspectionMediaUploadIds": ["guid1", "guid2"]
  }
  ```
- **Response:** `201 Created` -> `WarehouseInspectionDto`
- **Ghi chu:** Co the dinh kem anh chup tinh trang hang

### Step 3: Review ket qua kiem tra
- **Method:** `POST /api/warehouse/inbound-shipments/{shipmentId}/review`
- **Auth:** Required (Permission: `Warehouse.Inspect`)
- **Request:**
  ```json
  {
    "decision": "accept | reject",
    "reason": "Ly do (bat buoc khi reject)"
  }
  ```
- **Response:** `200 OK` -> `WarehouseInspectionDto`

## Flow sau Inspection

### Neu Accept:
- Hang duoc chuyen sang trang thai cho luu tru
- Nhan vien kho co the assign storage location

### Neu Reject:
- Hang bi tu choi, can gui tra cho seller
- Seller nhan thong bao

### Neu Condition khac voi mo ta:
- Seller nhan yeu cau xac nhan condition moi
- **Method:** `POST /api/items/{itemId}/confirm-inspected-condition`
- Seller xac nhan hoac tu choi condition moi

## Luu y nghiep vu

- Inspection la bat buoc truoc khi san pham co the duoc dau gia
- Anh chup kiem tra duoc luu kem de co bang chung
- Ket qua kiem tra anh huong den condition hien thi cua san pham
- Review la buoc thu hai de dam bao chat luong kiem tra
