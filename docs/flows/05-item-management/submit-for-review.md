# Gui Duyet San pham (Submit Item for Review)

## Tong quan
Seller gui san pham de admin duyet. Co 2 che do: duyet online (chi admin review) va duyet vat ly (gui den kho OIO de kiem tra). Sau khi submit, item chuyen tu `Draft` sang `PendingReview` (hoac `PendingInspection`).

## Actors
- **Seller** (can quyen `Items.Create`)

## Endpoint Sequence

### Step 1: Submit item

- **Method:** `POST /api/items/{itemId}/submit`
- **Auth:** Required - Permission `Items.Create`
- **Request:**
  ```json
  {
    "verifyByPlatform": "boolean (default: false)"
  }
  ```
  - `verifyByPlatform = false`: Chi can admin review anh/mo ta (duyet online)
  - `verifyByPlatform = true`: Seller gui hang den kho OIO de kiem tra tinh trang thuc te
- **Response:** `204 No Content`
- **Loi co the xay ra:**
  - `404 Not Found` - Item khong ton tai
  - `400 Bad Request` - Item khong o trang thai `Draft`
  - `400 Bad Request` - Thieu thong tin bat buoc (title, condition, media, v.v.)
  - `403 Forbidden` - Item khong thuoc user hien tai

### Step 2: Gui lai sau khi bi reject

- **Method:** `POST /api/items/{itemId}/resubmit`
- **Auth:** Required - Permission `Items.Resubmit`
- **Request:**
  ```json
  {
    "verifyByPlatform": "boolean (default: false)"
  }
  ```
- **Response:** `204 No Content`
- **Loi co the xay ra:**
  - `404 Not Found` - Item khong ton tai
  - `400 Bad Request` - Item khong o trang thai `Rejected`
  - `403 Forbidden` - Item khong thuoc user hien tai

## Business Logic

### Submit Handler

1. **Tim item** theo `itemId`, include Media, kiem tra ownership
2. **Kiem tra trang thai:** Phai o `Draft`
3. **Validate day du:**
   - Title khong rong
   - Condition hop le
   - Co it nhat 1 anh
   - Co primary image
4. **Chuyen trang thai:**
   - Neu `verifyByPlatform = false` -> `PendingReview`
   - Neu `verifyByPlatform = true` -> `PendingInspection`
5. **Raise event:** `ItemSubmittedEvent(itemId, auctionId, verifyByPlatform, nowUtc)`
6. **Luu:** `SaveChangesAsync`

### Resubmit Handler

1. **Tim item** theo `itemId`, kiem tra ownership
2. **Kiem tra trang thai:** Phai o `Rejected`
3. **Validate:** Tuong tu submit
4. **Chuyen trang thai:** `PendingReview` (hoac `PendingInspection` neu `verifyByPlatform = true`)
5. **Luu:** `SaveChangesAsync`

## Domain Events & Side Effects

| Event | Mo ta |
|-------|-------|
| `ItemSubmittedEvent` | Phat ra khi item duoc submit. Chua `verifyByPlatform` flag |
| `ItemStatusChangedEvent` | Khi trang thai item thay doi tu `Draft` sang `PendingReview` |

### Event Handlers (Side Effects)

- **Thong bao admin:** Gui notification cho admin rang co item moi can duyet
- **Neu `verifyByPlatform`:** Co the tu dong tao inbound shipment de seller gui hang den kho

## Luong Verify By Platform

```
[Draft]
    |
    | submit(verifyByPlatform=true)
    v
[PendingInspection]
    |
    | Seller gui hang den kho OIO (xem item-shipping.md)
    v
[ShippedToWarehouse]
    |
    | Kho nhan hang va kiem tra (xem item-qa.md)
    v
[InspectionCompleted]
    |
    | Seller xac nhan tinh trang (xem item-qa.md)
    v
[ConditionConfirmed]
    |
    | Tao Auction
    v
[Active]
```

## Luu y nghiep vu

- **Submit la hanh dong mot chieu** - Sau khi submit, item khong the chinh sua (them/xoa media, thay doi thong tin)
- **verifyByPlatform** la tinh nang cao cap: hang duoc gui den kho OIO de staff kiem tra thuc te -> tang do tin cay cho nguoi mua
- **Resubmit** chi kha dung khi item da bi `Rejected` - cho phep seller sua va gui lai
- **Validation truoc submit** dam bao item co du thong tin va media de admin co the danh gia
- **Seller can xem ky ly do reject** truoc khi resubmit de tranh bi reject lai
