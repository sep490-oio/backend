# Kiem tra Chat luong (Item QA / Inspection)

## Tong quan
Khi item duoc gui den kho OIO (verify by platform), staff kho se kiem tra tinh trang thuc te cua san pham. Sau kiem tra, seller xac nhan tinh trang da kiem tra de tien hanh dau gia.

## Actors
- **Warehouse Staff** (kiem tra hang hoa)
- **Seller** (xac nhan ket qua kiem tra)

## Luong xu ly

```
[Hang den kho]
     |
     | Staff nhan hang, tao warehouse item
     v
[InspectionQueue]
     |
     | POST /api/warehouse/inbound-shipments/{id}/inspect
     v
[Inspected]
     |
     | Staff/Supervisor review
     | POST /api/warehouse/inbound-shipments/{id}/review
     v
[InspectionCompleted]
     |
     | Seller xac nhan tinh trang
     | POST /api/items/{itemId}/confirm-inspected-condition
     v
[ConditionConfirmed]
     |
     | POST /api/items/{itemId}/activate
     v
[Active]
```

## Endpoint Sequence

### Warehouse Staff Endpoints

#### Xem hang doi kiem tra

- **Method:** `GET /api/warehouse/inbound-shipments/inspection-queue`
- **Auth:** Required (Warehouse Staff)
- **Response:** `200 OK` (danh sach item can kiem tra)

#### Kiem tra hang hoa

- **Method:** `POST /api/warehouse/inbound-shipments/{shipmentId}/inspect`
- **Auth:** Required (Warehouse Staff)
- **Request:** Bao gom ket qua kiem tra, anh chup, ghi chu
- **Response:** `200 OK`

#### Review kiem tra

- **Method:** `POST /api/warehouse/inbound-shipments/{shipmentId}/review`
- **Auth:** Required (Warehouse Supervisor)
- **Request:** Chap thuan hoac yeu cau kiem tra lai
- **Response:** `200 OK`

### Seller Endpoint

#### Xac nhan tinh trang da kiem tra

- **Method:** `POST /api/items/{itemId}/confirm-inspected-condition`
- **Auth:** Required - Permission `Items.Resubmit`
- **Request:** Khong co body
- **Response:** `200 OK`
  ```json
  {
    "id": "guid",
    "title": "string",
    "condition": "string",
    "status": "ConditionConfirmed",
    "inspectionResult": {
      "inspectedCondition": "string",
      "notes": "string?",
      "inspectedAt": "datetime"
    }
  }
  ```
- **Loi co the xay ra:**
  - `404 Not Found` - Item khong ton tai
  - `400 Bad Request` - Item khong o trang thai `InspectionCompleted`
  - `403 Forbidden` - Item khong thuoc user hien tai
  - `409 Conflict` - Da xac nhan truoc do

## Business Logic

### Confirm Inspected Condition Handler

1. **Tim item** theo `itemId`, kiem tra ownership
2. **Kiem tra trang thai:** Item phai o `InspectionCompleted`
3. **Xac nhan:** `item.ConfirmInspectedCondition(nowUtc)` -> Trang thai chuyen sang `ConditionConfirmed`
4. **Luu:** `SaveChangesAsync`

## Quy trinh Kiem tra tai Kho

1. **Nhan hang:** Staff scan va ghi nhan shipment den kho
2. **Kiem tra vat ly:**
   - Doi chieu tinh trang thuc te voi mo ta cua seller
   - Chup anh bang chung
   - Ghi nhan ket qua: condition (`new`, `like_new`, `very_good`, `good`, `acceptable`, `damaged`)
   - Ghi chu bat thuong (neu co)
3. **Review:** Supervisor xem xet ket qua kiem tra
4. **Thong bao seller:** Gui notification kem ket qua kiem tra
5. **Seller xac nhan:** Seller xem ket qua va xac nhan dong y

## Warehouse Domain Events

| Event | Mo ta |
|-------|-------|
| `WarehouseItemInspectedEvent` | Khi item duoc kiem tra xong |
| `InspectionReviewedEvent` | Khi supervisor review ket qua |

## Luu y nghiep vu

- **Kiem tra vat ly** dam bao san pham dung nhu mo ta - tang do tin cay cho nguoi mua
- **Neu condition khac voi mo ta cua seller:** Staff ghi nhan condition thuc te. Seller co the thay doi mo ta hoac tu choi
- **Anh chup bang chung** duoc upload qua luong Media Upload voi context `warehouse_inspection_image`
- **Seller phai xac nhan** de dong y voi ket qua kiem tra truoc khi item co the duoc kich hoat
- **Neu seller khong dong y:** Co the tao dispute hoac yeu cau gui tra hang
- **Sau khi xac nhan:** Item chuyen sang `ConditionConfirmed`, seller co the `activate` va tao auction
- **Chi ap dung** cho item co `verifyByPlatform = true`
- **Luong thong thuong** (khong verify by platform): `Draft -> PendingReview -> Approved -> Active`
- **Luong verify by platform:** `Draft -> PendingInspection -> ShippedToWarehouse -> InspectionCompleted -> ConditionConfirmed -> Active`
