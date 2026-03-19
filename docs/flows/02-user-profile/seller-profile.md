# Ho so Nguoi ban (Seller Profile)

## Tong quan
Nguoi dung tao ho so nguoi ban de co the dang san pham dau gia. Ho so can duoc Admin xac minh truoc khi co the ban hang.

## Actors
- **Nguoi dung da dang nhap** (tao/xem/cap nhat)
- **Admin** (xac minh/tu choi - xem [03-seller-verification](../03-seller-verification/README.md))

## Endpoint Sequence

### Step 1: Tao ho so nguoi ban

- **Method:** `POST /api/me/seller-profile`
- **Auth:** Required - Permission `Me.ManageSellerProfile`
- **Request:**
  ```json
  {
    "storeName": "string (required - ten cua hang)",
    "storeDescription": "string (required - mo ta cua hang)"
  }
  ```
- **Response:** `201 Created`
  ```json
  {
    "id": "guid",
    "storeName": "string",
    "storeDescription": "string",
    "status": "Pending",
    "createdAt": "datetime"
  }
  ```
- **Loi co the xay ra:**
  - `409 Conflict` - Da co seller profile
  - `401 Unauthorized` - Chua dang nhap
  - `403 Forbidden` - Khong co quyen

### Step 2: Xem ho so nguoi ban

- **Method:** `GET /api/me/seller-profile`
- **Auth:** Required - Permission `Me.ReadSellerProfile`
- **Response:** `200 OK`
  ```json
  {
    "id": "guid",
    "storeName": "string",
    "storeDescription": "string",
    "status": "Pending | Verified | Rejected",
    "verifiedAt": "datetime?",
    "rejectedAt": "datetime?",
    "createdAt": "datetime"
  }
  ```
- **Loi co the xay ra:**
  - `404 Not Found` - Chua tao seller profile

### Step 3: Cap nhat ho so nguoi ban

- **Method:** `PUT /api/me/seller-profile`
- **Auth:** Required - Permission `Me.ManageSellerProfile`
- **Request:**
  ```json
  {
    "storeName": "string (required)",
    "storeDescription": "string (required)"
  }
  ```
- **Response:** `200 OK`
- **Loi co the xay ra:**
  - `404 Not Found` - Chua tao seller profile
  - `409 Conflict` - Khong the cap nhat khi dang duoc review

## Admin Endpoints

### Xem danh sach ho so nguoi ban

- **Method:** `GET /api/admin/seller-profiles`
- **Auth:** Required - Permission `Admin.ManageSellerProfiles`

### Xac minh ho so nguoi ban

- **Method:** `POST /api/admin/seller-profiles/{id}/verify`
- **Auth:** Required - Permission `Admin.ManageSellerProfiles`
- **Response:** `204 No Content`

### Tu choi ho so nguoi ban

- **Method:** `POST /api/admin/seller-profiles/{id}/reject`
- **Auth:** Required - Permission `Admin.ManageSellerProfiles`
- **Response:** `204 No Content`

## State Machine

```
[Chua tao]
    |
    | POST /api/me/seller-profile
    v
[Pending]
    |
    +-- Admin Verify --> [Verified] --> Co the tao Item / Auction
    |
    +-- Admin Reject --> [Rejected]
                            |
                            +-- User Update --> [Pending] (gui lai)
```

## Luu y nghiep vu

- **Moi user chi co 1 seller profile** - tao lan 2 se bi `409 Conflict`
- **Seller profile** phai duoc Admin **verify** truoc khi user co the tao san pham dau gia
- **Sau khi bi reject**, user co the cap nhat thong tin va profile se chuyen lai trang thai `Pending`
- **StoreName** la ten cua hang hien thi cong khai tren platform
- **StoreDescription** la mo ta ve cua hang, loai san pham kinh doanh
- Seller profile la dieu kien tien quyet cho Identity Verification (eKYC)
- Khi duoc verify, user se duoc gan them role `Seller`

## Trust Score

### Tong quan

Moi seller co mot diem tin cay (`TrustScore`) duoc tinh tu dong boi `SellerTrustScoreCalculator`. Diem nay phan anh muc do tin cay cua seller dua tren nhieu yeu to.

### Cac truong trong SellerProfile

| Truong | Kieu | Mo ta |
|---|---|---|
| `TrustScoreOverall` | `decimal` | Diem tin cay tong (0 - 100), duoc clamp trong khoang [0, 100] |
| `TrustScoreCalculatedAt` | `DateTime?` | Thoi diem tinh lan cuoi |

### Cac truong trong DTO

| DTO | Truong | Mo ta |
|---|---|---|
| `SellerProfileDto` | `TrustScore`, `TrustScoreCalculatedAt` | Day du thong tin cho owner/admin |
| `PublicSellerProfileDto` | `TrustScore` | Chi diem tong, hien thi cong khai |

### Cong thuc tinh diem (5 thanh phan)

| # | Thanh phan | Trong so | Cong thuc |
|---|---|---|---|
| 1 | **Rating** | 30% | `AverageRating / 5.0 * 100`. Mac dinh 50 neu chua co review |
| 2 | **Completion** | 25% | `CompletedOrders / TotalOrders * 100`. Mac dinh 50 neu chua co order |
| 3 | **Dispute** | 20% | `(1 - OpenDisputes / TotalOrders) * 100`. Mac dinh 50 neu chua co order. Toi thieu 0 |
| 4 | **Verification** | 15% | Identity Verification Approved = 100, co AutoVerifyScore thi dung gia tri do, con lai = 50 |
| 5 | **Risk** | 10% | Khong co risk flag = 100, Low = 80, Medium = 50, High = 20, Critical = 0 |

**Cong thuc tong:**
```
Overall = Rating * 0.30 + Completion * 0.25 + Dispute * 0.20 + Verification * 0.15 + Risk * 0.10
```

Ket qua duoc `Math.Clamp(overall, 0, 100)`.

### Lich tinh lai

- **Quartz Job:** `RecalculateSellerTrustScoresJob`
- **Tan suat:** Moi 6 gio (`[DisallowConcurrentExecution]`)
- **Doi tuong:** Tat ca seller co `Status = Verified`
- Loi tinh cho tung seller duoc log warning nhung khong lam dung toan bo job
