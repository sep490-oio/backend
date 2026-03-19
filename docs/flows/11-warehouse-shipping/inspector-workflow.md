# Inspector Workflow — Quy trinh kiem dinh hang hoa

## Tong quan

Inspector (nhan vien kiem dinh kho) la role chuyen trach voi nhiem vu: tiep nhan hang den kho, kiem tra tinh trang thuc te, ghi nhan ket qua bang hinh anh, review quyet dinh, va luu tru hang vao vi tri kho. Day la flow end-to-end tu goc nhin inspector.

## Actor

- **Inspector** — role `inspector`, priority 60

## Permissions

| Permission | Mo ta |
|-----------|-------|
| `warehouse:shipments:read` | Xem danh sach hang cho kiem dinh (inspection queue) |
| `warehouse:item:inspect` | Tien hanh kiem dinh va review ket qua |
| `warehouse:item:store` | Gan hang vao vi tri luu tru trong kho |

Ngoai ra inspector co cac quyen co ban: `users:me:*`, `media:upload`, `media:upload:confirm`, `media:contexts:read`.

---

## State Machines

### InboundShipment Lifecycle

```
AwaitingPickup → InTransit → Arrived → Inspected → Completed
                                  ↘ Cancelled | Failed
```

Inspector tham gia tu `Arrived` → `Inspected` → `Completed`.

### WarehouseItem Lifecycle

```
Pending → Received → Inspected → Stored → Reserved → Dispatched
```

Inspector tham gia tu `Pending` → `Received` → `Inspected` → `Stored`.

### WarehouseInspection DecisionStatus

```
PendingReview
    ├── Approved                        (condition khop → item tiep tuc auction)
    ├── Rejected                        (item bi tu choi)
    └── ConditionConfirmationRequired   (condition khac → cho seller xac nhan)
              └── ConditionConfirmed    (seller dong y → item tiep tuc auction)
```

---

## Flow Chi Tiet

### Step 1: Nhan thong bao hang den kho

Khi InboundShipment chuyen sang `Arrived`, he thong tu dong gui notification den tat ca user co role `Inspector` hoac `Admin`.

**Notification:**
- Type: `inbound_shipment_arrived_for_inspection`
- Title: "Co hang cho kiem dinh"
- Message: `Item "{title}" has arrived at warehouse and is waiting for inspection.`
- Priority: Normal
- Metadata: `inboundShipmentId`, `itemId`, `carrierTrackingNumber`

**Event:** `InboundShipmentArrivedEvent` → `InboundShipmentArrivedEventHandler`

---

### Step 2: Xem hang doi kiem dinh (Inspection Queue)

- **Method:** `GET /api/warehouse/inbound-shipments/inspection-queue`
- **Permission:** `warehouse:shipments:read`
- **Query Parameters:**
  - `page` (int, default: 1)
  - `pageSize` (int, default: 20)
- **Response:** `200 OK`
  ```json
  [
    {
      "inboundShipmentId": "guid",
      "itemId": "guid",
      "itemTitle": "string",
      "sellerId": "guid",
      "warehouseItemId": "guid | null",
      "inspectionId": "guid | null",
      "shipmentStatus": "arrived | inspected",
      "queueStatus": "awaiting_inspection | awaiting_review",
      "carrierTrackingNumber": "string | null",
      "arrivedAt": "datetime | null",
      "declaredCondition": "string",
      "conditionOnArrival": "string | null",
      "inspectedAt": "datetime | null"
    }
  ]
  ```

**Queue Status Logic:**
| ShipmentStatus | Inspection | QueueStatus |
|---------------|------------|-------------|
| `arrived` | Chua co | `awaiting_inspection` |
| `inspected` | `pending_review` | `awaiting_review` |

Items khong thuoc 2 truong hop tren bi loai khoi queue.

**Sap xep:** `ArrivedAt` giam dan → `CreatedAt` giam dan (hang den truoc hien truoc).

---

### Step 3: Kiem dinh hang hoa

- **Method:** `POST /api/warehouse/inbound-shipments/{shipmentId:guid}/inspect`
- **Permission:** `warehouse:item:inspect`
- **Request:**
  ```json
  {
    "condition": "string (required)",
    "inspectionNotes": "string | null",
    "inspectionMediaUploadIds": ["guid", "guid", "..."]
  }
  ```

**Condition values hop le:**
| Value | Mo ta |
|-------|-------|
| `new` | Moi nguyen seal |
| `like_new` | Nhu moi, khong co dau hieu su dung |
| `very_good` | Rat tot, co dau hieu su dung nhe |
| `good` | Tot, co dau hieu su dung ro |
| `acceptable` | Chap nhan duoc, co hao mon |
| `damaged` | Hu hong |

- **Response:** `201 Created`
  ```json
  {
    "id": "guid",
    "warehouseItemId": "guid",
    "inboundShipmentId": "guid",
    "itemId": "guid",
    "declaredCondition": "string (seller khai bao)",
    "conditionOnArrival": "string (inspector danh gia)",
    "inspectionNotes": "string | null",
    "decisionStatus": "pending_review",
    "decisionReason": null,
    "inspectedBy": "guid (inspector userId)",
    "inspectedAt": "datetime",
    "reviewedBy": null,
    "reviewedAt": null,
    "sellerConfirmedAt": null,
    "createdAt": "datetime",
    "modifiedAt": null,
    "evidence": [
      {
        "publicId": "string",
        "folder": "string",
        "secureUrl": "string",
        "fileName": "string | null",
        "bytes": 12345,
        "format": "jpg",
        "width": 1600,
        "height": 1200,
        "durationSeconds": null
      }
    ]
  }
  ```

**Business Logic (InspectWarehouseItemCommandHandler):**
1. Validate co it nhat 1 media upload (bat buoc chup anh)
2. Load InboundShipment — phai o status `arrived`
3. Kiem tra chua co inspection record nao
4. Load Item — lay thong tin declared condition
5. Validate condition value hop le
6. Validate tung media upload:
   - Upload phai ton tai
   - Upload phai thuoc inspector (current user)
   - Upload phai da confirmed
   - Upload phai co context `warehouse_inspection_image`
   - Upload chua duoc link cho entity khac
7. Tao/lay WarehouseItem → mark `Received` → mark `Inspected`
8. Tao WarehouseInspection record voi evidence snapshots
9. Chuyen InboundShipment sang `Inspected`
10. Link media uploads vao inspection
11. Gui notification cho seller

**Loi co the xay ra:**
| Error | Mo ta |
|-------|-------|
| `WarehouseInspection.EvidenceRequired` | Khong co anh kem theo |
| `InboundShipment.NotFound` | Shipment khong ton tai |
| `InboundShipment.CannotInspect` | Shipment khong o status `arrived` |
| `WarehouseInspection.AlreadyExists` | Da co inspection record |
| `Item.NotFound` | Item khong ton tai |
| `Warehouse.InvalidCondition` | Condition value khong hop le |
| Media validation errors | Upload khong ton tai / khong phai cua ban / chua confirm / sai context / da link |

---

### Step 4: Review ket qua kiem dinh

- **Method:** `POST /api/warehouse/inbound-shipments/{shipmentId:guid}/review`
- **Permission:** `warehouse:item:inspect`
- **Request:**
  ```json
  {
    "decision": "approve | reject",
    "reason": "string | null (bat buoc khi reject)"
  }
  ```
- **Response:** `200 OK` — `WarehouseInspectionDto` (cau truc giong Step 3)

**Preconditions:**
- Inspection phai o status `pending_review`
- Item phai o status `PendingVerify`

**3 nhanh quyet dinh:**

#### 4a. Reject — Tu choi

- `reason` bat buoc (khong duoc rong)
- Inspection → `rejected`
- Item → `PendingReject` (khong the tien hanh auction)
- Notification: "San pham bi tu choi sau kiem dinh" — **HIGH priority** → seller

#### 4b. Approve — Condition khop voi seller khai bao

- `conditionOnArrival` == `declaredCondition` (sau khi map sang ItemCondition)
- Inspection → `approved`
- Item → `VerifiedFromPlatform` (xac nhan tu nen tang)
- Tu dong goi `continuationService.ContinueAsync()` — co the auto-continue auction
- Notification: thong bao duyet + trang thai auction → seller

#### 4c. Approve — Condition KHAC voi seller khai bao

- `conditionOnArrival` != `declaredCondition`
- Inspection → `condition_confirmation_required`
- Item → `PendingConditionConfirmation`
- Notification: "Can xac nhan tinh trang sau kiem dinh" — **HIGH priority** → seller
- **Cho seller phan hoi** (xem Step 4d)

#### 4d. Seller xac nhan condition moi (khong phai inspector thuc hien)

- **Method:** `POST /api/items/{itemId}/confirm-inspected-condition`
- **Permission:** Seller (chu so huu item)
- Inspection → `condition_confirmed`
- Item cap nhat condition moi
- Tu dong continue auction
- Notification: xac nhan thanh cong → seller

**Loi co the xay ra:**
| Error | Mo ta |
|-------|-------|
| `WarehouseInspection.NotFound` | Inspection khong ton tai |
| `WarehouseInspection.AlreadyReviewed` | Da review roi |
| `Item.NotFound` | Item khong ton tai |
| `WarehouseInspection.InvalidItemState` | Item khong o status `PendingVerify` |
| `WarehouseInspection.ReasonRequired` | Reject ma khong co reason |
| `WarehouseInspection.UnsupportedApprovalCondition` | Condition khong the map sang ItemCondition |

---

### Step 5: Luu tru hang vao kho

- **Method:** `POST /api/warehouse/warehouse-items/{warehouseItemId}/store`
- **Permission:** `warehouse:item:store`
- **Request:**
  ```json
  {
    "storageLocationId": "guid (required)"
  }
  ```
- **Response:** `200 OK`
  ```json
  {
    "id": "guid",
    "itemId": "guid",
    "inboundShipmentId": "guid",
    "storageLocationId": "guid",
    "status": "stored",
    "receivedAt": "datetime",
    "createdAt": "datetime",
    "modifiedAt": "datetime"
  }
  ```

**Business Logic (StoreWarehouseItemCommandHandler):**
1. Load WarehouseItem — phai o status `inspected`
2. Load StorageLocation — phai ton tai
3. Kiem tra location chua bi chiem (`!IsOccupied`)
4. Gan item vao location → WarehouseItem status → `stored`
5. Mark StorageLocation la `Occupied`
6. Load InboundShipment → chuyen sang `completed`
7. Raises `WarehouseItemStoredEvent`

**Loi co the xay ra:**
| Error | Mo ta |
|-------|-------|
| `WarehouseItem.NotFound` | Warehouse item khong ton tai |
| `WarehouseItem.NotInspected` | Chua kiem dinh xong |
| `StorageLocation.NotFound` | Vi tri kho khong ton tai |
| `StorageLocation.Occupied` | Vi tri da co hang khac |

---

## End-to-End Flow Diagram

```
[Shipment Arrived]
       |
       | InboundShipmentArrivedEvent → Notification to Inspectors
       v
[Inspector: Xem Inspection Queue]
  GET /api/warehouse/inbound-shipments/inspection-queue
       |
       | Chon shipment co queueStatus = "awaiting_inspection"
       v
[Inspector: Upload anh kiem dinh]
  POST /api/media/upload-signature  (context: warehouse_inspection_image)
  → Client upload to Cloudinary
  POST /api/media/confirm
       |
       v
[Inspector: Tien hanh kiem dinh]
  POST /api/warehouse/inbound-shipments/{id}/inspect
  → WarehouseItem: Received → Inspected
  → InboundShipment: Arrived → Inspected
  → Inspection: PendingReview
       |
       v
[Inspector: Review ket qua]
  POST /api/warehouse/inbound-shipments/{id}/review
       |
       +--[Reject]---→ Item: PendingReject          → Notification (HIGH) → Seller
       |
       +--[Approve, condition khop]---→ Item: VerifiedFromPlatform
       |                                    → Continue auction
       |                                    → Notification → Seller
       |
       +--[Approve, condition khac]---→ Item: PendingConditionConfirmation
                                            → Notification (HIGH) → Seller
                                            → Seller: POST /api/items/{id}/confirm-inspected-condition
                                            → Continue auction
       |
       v
[Inspector: Luu tru vao kho]
  POST /api/warehouse/warehouse-items/{id}/store
  → WarehouseItem: Inspected → Stored
  → InboundShipment: Inspected → Completed
  → StorageLocation: Occupied
```

---

## Media Requirements

Inspector **bat buoc** phai chup anh khi kiem dinh:
- Upload context: `warehouse_inspection_image`
- Resource type: `image`
- Allowed formats: `jpg`, `jpeg`, `png`, `webp`
- Max file size: 10 MB
- Eager transform: `w_1600,h_1600,c_limit`
- Max uploads per entity: 10
- Evidence duoc snapshot thanh JSON array trong WarehouseInspection record

---

## Domain Events

| Event | Trigger | Side Effect |
|-------|---------|-------------|
| `InboundShipmentArrivedEvent` | Shipment den kho | Notification → Inspectors + Admins |
| `WarehouseItemCreatedEvent` | Item duoc tiep nhan | — |
| `WarehouseItemStoredEvent` | Item duoc luu tru | — |
| `WarehouseItemReservedEvent` | Item duoc reserve cho outbound | — |
| `WarehouseItemDispatchedEvent` | Item duoc gui di | — |

---

## Luu y nghiep vu

- Mot shipment chi co the co **1 inspection record** — khong the kiem dinh lai
- Review dung chung permission `warehouse:item:inspect` voi inspect — inspector tu review ket qua cua minh
- Khi condition khac nhau, he thong **khong tu reject** ma cho seller quyet dinh co dong y condition moi khong
- Sau khi `Store`, InboundShipment tu dong `Completed` — khong can thao tac them
- Inspector khong truc tiep tao WarehouseItem — he thong tu dong tao khi inspect
