# Duyet San Pham (Item Review)

## Tong quan

Truoc khi san pham co the duoc dau gia, admin phai duyet noi dung (tieu de, mo ta, anh, ...). He thong ho tro hang doi duyet, phan cong reviewer, phe duyet/tu choi, va xem lich su review.

## Actors

- **Admin** - nguoi co quyen `ManageItems`
- **Seller** - nguoi ban, nhan ket qua duyet

## Endpoint Sequence

### Step 1: Xem hang doi duyet
- **Method:** `GET /api/admin/items/review-queue`
- **Auth:** Required (Permission: `ManageItems`)
- **Response:** `200 OK` -> `ReviewQueueItemDto[]`

### Step 2: Xem chi tiet san pham
- **Method:** `GET /api/admin/items/{itemId}`
- **Auth:** Required (Permission: `ManageItems`)
- **Response:** `200 OK`

### Step 3: Phan cong reviewer
- **Method:** `POST /api/admin/items/{itemId}/assign`
- **Auth:** Required (Permission: `ManageItems`)
- **Ghi chu:** Gan admin cu the de duyet item

### Step 4a: Phe duyet
- **Method:** `POST /api/admin/items/{itemId}/approve`
- **Auth:** Required (Permission: `ManageItems`)
- **Response:** `200 OK`
- **Ghi chu:** Item chuyen sang trang thai Approved, san sang tao auction

### Step 4b: Tu choi
- **Method:** `POST /api/admin/items/{itemId}/reject`
- **Auth:** Required (Permission: `ManageItems`)
- **Response:** `200 OK`
- **Ghi chu:** Item chuyen sang trang thai Rejected voi ly do

### Step 5: Xem lich su review
- **Method:** `GET /api/admin/items/{itemId}/reviews`
- **Auth:** Required (Permission: `ManageItems`)
- **Response:** `200 OK`

## Domain Events & Side Effects

- `AuctionApprovedEvent` -> Notification seller: "San pham da duoc phe duyet"
- `AuctionRejectedEvent` -> Notification seller: "San pham can chinh sua" (Priority: High)

## Item Review Status

```
Draft -> Submitted -> InReview -> Approved | Rejected
                                            -> [Resubmit] -> Submitted -> ...
```

## Luu y nghiep vu

- Seller phai submit item truoc khi vao hang doi duyet
- Admin co the assign reviewer hoac tu pick
- Tu choi phai co ly do, seller co the chinh sua va resubmit
- So lan reject duoc ghi nhan (rejectionCount)
- Lich su review luu lai tat ca quyet dinh de audit
