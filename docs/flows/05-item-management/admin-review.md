# Admin Duyet San pham (Admin Item Review)

## Tong quan
Admin duyet cac san pham duoc seller gui len. Admin co the xem hang doi duyet, chi tiet item, lich su review, gan reviewer, chap thuan hoac tu choi.

## Actors
- **Admin** (can quyen `Admin.ReadItems` de xem, `Admin.ManageItems` de duyet)

## Endpoint Sequence

### Step 1: Xem hang doi duyet

- **Method:** `GET /api/admin/items/review-queue`
- **Auth:** Required - Permission `Admin.ReadItems`
- **Query Parameters:**
  ```
  ?status=PendingReview
  &assignedTo=guid      (loc theo reviewer)
  &page=1
  &pageSize=10
  ```
- **Response:** `200 OK`
  ```json
  {
    "items": [
      {
        "id": "guid",
        "title": "string",
        "condition": "string",
        "status": "PendingReview",
        "sellerId": "guid",
        "sellerName": "string",
        "assignedReviewerId": "guid?",
        "submittedAt": "datetime",
        "createdAt": "datetime",
        "mediaCount": "int"
      }
    ],
    "totalCount": 15,
    "page": 1,
    "pageSize": 10
  }
  ```

### Step 2: Xem chi tiet item

- **Method:** `GET /api/admin/items/{itemId}`
- **Auth:** Required - Permission `Admin.ReadItems`
- **Response:** `200 OK` (thong tin day du item, bao gom media, seller info, attributes)

### Step 3: Xem lich su review

- **Method:** `GET /api/admin/items/{itemId}/reviews`
- **Auth:** Required - Permission `Admin.ReadItems`
- **Response:** `200 OK` (danh sach cac lan review, bao gom nguoi review, ket qua, ly do)

### Step 4: Gan reviewer

- **Method:** `POST /api/admin/items/{itemId}/assign`
- **Auth:** Required - Permission `Admin.ManageItems`
- **Request:**
  ```json
  {
    "adminId": "guid (required - ID cua admin duoc gan)"
  }
  ```
- **Response:** `204 No Content`
- **Loi co the xay ra:**
  - `404 Not Found` - Item khong ton tai
  - `400 Bad Request` - Item khong o trang thai `PendingReview`

### Step 5a: Chap thuan item

- **Method:** `POST /api/admin/items/{itemId}/approve`
- **Auth:** Required - Permission `Admin.ManageItems`
- **Request:** Khong co body
- **Response:** `204 No Content`
- **Loi co the xay ra:**
  - `404 Not Found` - Item khong ton tai
  - `400 Bad Request` - Item khong o trang thai `PendingReview`

### Step 5b: Tu choi item

- **Method:** `POST /api/admin/items/{itemId}/reject`
- **Auth:** Required - Permission `Admin.ManageItems`
- **Request:**
  ```json
  {
    "reason": "string (required - ly do tu choi)"
  }
  ```
- **Response:** `204 No Content`
- **Loi co the xay ra:**
  - `404 Not Found` - Item khong ton tai
  - `400 Bad Request` - Item khong o trang thai `PendingReview`

## Business Logic

### Approve Handler

1. **Tim item** theo `itemId`, include review history
2. **Kiem tra trang thai:** Phai o `PendingReview`
3. **Approve:** `item.Approve(adminId, nowUtc)` -> Trang thai chuyen sang `Approved`
4. **Tao review record:** Ghi nhan nguoi duyet, thoi gian, ket qua
5. **Raise event:** `ItemApprovedEvent(itemId, auctionId, reviewerId, nowUtc)`
6. **Luu:** `SaveChangesAsync`

### Reject Handler

1. **Tim item** theo `itemId`, include review history
2. **Kiem tra trang thai:** Phai o `PendingReview`
3. **Reject:** `item.Reject(adminId, reason, nowUtc)` -> Trang thai chuyen sang `Rejected`
4. **Tao review record:** Ghi nhan nguoi duyet, thoi gian, ket qua, ly do
5. **Raise event:** `ItemRejectedEvent(itemId, auctionId, reviewerId, reason, nowUtc)`
6. **Luu:** `SaveChangesAsync`

### Assign Reviewer Handler

1. **Tim item** theo `itemId`
2. **Kiem tra trang thai:** Phai o `PendingReview`
3. **Gan reviewer:** `item.AssignReviewer(adminId, nowUtc)`
4. **Luu:** `SaveChangesAsync`

## Domain Events & Side Effects

| Event | Mo ta |
|-------|-------|
| `ItemApprovedEvent` | Gui notification cho seller. Item san sang de activate va tao auction |
| `ItemRejectedEvent` | Gui notification cho seller kem ly do tu choi. Seller co the resubmit |
| `ItemStatusChangedEvent` | Ghi nhan thay doi trang thai de audit |

## Luu y nghiep vu

- **Review queue** cho phep loc va sap xep theo nhieu tieu chi (trang thai, nguoi review, ngay gui)
- **Assign reviewer** khong bat buoc - bat ky admin nao co quyen deu co the approve/reject
- **Ly do reject** nen cu the, giup seller biet can sua gi truoc khi resubmit
- **Lich su review** giu tat ca cac lan duyet, bao gom ca approve va reject -> de audit
- **Admin can kiem tra:** Mo ta, anh/video, condition, attributes phu hop voi thuc te
- **Sau khi approve:** Item chuyen sang `Approved`, seller co the `activate` de tao auction
- **Sau khi reject:** Item chuyen sang `Rejected`, seller co the sua va `resubmit`
