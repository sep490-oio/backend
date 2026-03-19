# Luong Tra Hang (Order Return)

## Tong quan

Buyer co the yeu cau tra hang trong thoi gian decision window. Luong tra hang gom: yeu cau tra hang -> seller duyet/tu choi -> buyer gui hang tra -> seller xac nhan nhan hang -> hoan tien.

## Actors

- **Buyer** - yeu cau tra hang, gui hang tra
- **Seller** - duyet/tu choi, xac nhan nhan hang tra
- **System** - xu ly hoan tien

## Endpoint Sequence

### Step 1: Buyer yeu cau tra hang
- **Method:** `POST /api/orders/{orderId}/returns`
- **Auth:** Required (Buyer)
- **Request:**
  ```json
  {
    "reasonCode": "defective | not_as_described | wrong_item | damaged",
    "description": "Mo ta ly do tra hang"
  }
  ```
- **Response:** `200 OK` -> `OrderReturnDto`
- **Ghi chu:** Chi co the yeu cau tra hang khi order o trang thai `Delivered` va trong decision window

### Step 2: Seller duyet yeu cau tra hang
- **Method:** `POST /api/orders/{orderId}/returns/{returnId}/approve`
- **Auth:** Required (Seller)
- **Request:**
  ```json
  {
    "notes": "Chap nhan tra hang"
  }
  ```
- **Response:** `200 OK` -> `OrderReturnDto`

### Step 3 (Alt): Seller tu choi tra hang
- **Method:** `POST /api/orders/{orderId}/returns/{returnId}/reject`
- **Auth:** Required (Seller)
- **Request:**
  ```json
  {
    "reason": "Ly do tu choi"
  }
  ```
- **Response:** `200 OK` -> `OrderReturnDto`
- **Ghi chu:** Buyer co the mo dispute neu khong dong y

### Step 4: Buyer gui hang tra
- **Method:** `POST /api/orders/{orderId}/returns/{returnId}/ship`
- **Auth:** Required (Buyer)
- **Request:**
  ```json
  {
    "providerCode": "ghn",
    "trackingNumber": "GHN123456"
  }
  ```
- **Response:** `200 OK` -> `OrderReturnDto`

### Step 5: Seller xac nhan nhan hang tra
- **Method:** `POST /api/orders/{orderId}/returns/{returnId}/confirm-received`
- **Auth:** Required (Seller)
- **Response:** `200 OK` -> `OrderReturnDto`
- **Ghi chu:** Sau khi xac nhan, he thong xu ly hoan tien tu escrow

## Return Status (State Machine)

```
Requested -> Approved -> Shipped -> Received -> Resolved
         |
         -> Rejected (buyer co the mo dispute)
```

| Status | Mo ta |
|---|---|
| `Requested` | Buyer da gui yeu cau |
| `Approved` | Seller chap nhan |
| `Rejected` | Seller tu choi |
| `Shipped` | Buyer da gui hang tra |
| `Received` | Seller da nhan hang tra |
| `Resolved` | Da hoan tien |
| `Cancelled` | Buyer huy yeu cau |

## Domain Events & Side Effects

- Khi return duoc tao: escrow bi dong bang (khong giai ngan cho seller)
- Khi return resolve: escrow refund cho buyer
- `EscrowRefundedToBuyerDomainEvent` -> Notification buyer + Audit log

## Luu y nghiep vu

- Chi co the yeu cau tra hang trong decision window
- Moi order chi co the co 1 return request tai mot thoi diem
- Neu seller tu choi: buyer co the mo dispute de admin giai quyet
- Seller phai xac nhan nhan hang tra truoc khi he thong hoan tien
- Phi van chuyen tra hang do buyer chiu (tuy chinh sach)
