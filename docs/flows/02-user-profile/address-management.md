# Quan ly Dia chi (Address Management)

## Tong quan
Nguoi dung quan ly danh sach dia chi giao hang / nhan hang. Moi user co the co nhieu dia chi va dat 1 dia chi mac dinh.

## Actors
- **Nguoi dung da dang nhap**

## Endpoint Sequence

### Step 1: Xem danh sach dia chi

- **Method:** `GET /api/me/addresses`
- **Auth:** Required - Permission `Me.ReadAddress`
- **Query Parameters:**
  ```
  ?page=1
  &pageSize=10
  ```
- **Response:** `200 OK`
  ```json
  {
    "items": [
      {
        "id": "guid",
        "type": "string (Shipping / Billing / Both)",
        "recipientName": "string",
        "street": "string",
        "ward": "string",
        "district": "string",
        "city": "string",
        "postalCode": "string?",
        "phoneNumber": "string",
        "isDefault": "boolean",
        "createdAt": "datetime"
      }
    ],
    "totalCount": 3,
    "page": 1,
    "pageSize": 10
  }
  ```

### Step 2: Them dia chi moi

- **Method:** `POST /api/me/addresses`
- **Auth:** Required - Permission `Me.ManageAddress`
- **Request:**
  ```json
  {
    "type": "string (required - 'Shipping', 'Billing', 'Both')",
    "recipientName": "string (required)",
    "street": "string (required)",
    "ward": "string (required)",
    "district": "string (required)",
    "city": "string (required)",
    "postalCode": "string? (optional)",
    "phoneNumber": "string (required)",
    "countryCode": "string (default: 'VN')",
    "isDefault": "boolean (default: false)"
  }
  ```
- **Response:** `201 Created`
- **Loi co the xay ra:**
  - `422 Unprocessable Entity` - Validation loi

### Step 3: Cap nhat dia chi

- **Method:** `PUT /api/me/addresses/{addressId}`
- **Auth:** Required - Permission `Me.ManageAddress`
- **Request:**
  ```json
  {
    "type": "string? (optional)",
    "recipientName": "string? (optional)",
    "street": "string? (optional)",
    "ward": "string? (optional)",
    "district": "string? (optional)",
    "city": "string? (optional)",
    "phoneNumber": "string? (optional)",
    "countryCode": "string? (optional)",
    "postalCode": "string? (optional)"
  }
  ```
- **Response:** `200 OK`
- **Loi co the xay ra:**
  - `404 Not Found` - Dia chi khong ton tai
  - `403 Forbidden` - Dia chi khong thuoc user hien tai

### Step 4: Xoa dia chi

- **Method:** `DELETE /api/me/addresses/{addressId}`
- **Auth:** Required - Permission `Me.ManageAddress`
- **Response:** `204 No Content`
- **Loi co the xay ra:**
  - `404 Not Found` - Dia chi khong ton tai
  - `400 Bad Request` - Khong the xoa dia chi mac dinh dang duoc su dung

### Step 5: Dat dia chi mac dinh

- **Method:** `PATCH /api/me/addresses/{addressId}/default`
- **Auth:** Required - Permission `Me.ManageAddress`
- **Response:** `204 No Content`
- **Loi co the xay ra:**
  - `404 Not Found` - Dia chi khong ton tai

## Business Logic

### Add Address Handler

1. **Lay user hien tai** include `Addresses`
2. **Validate PhoneNumber:** `PhoneNumber.Create(phoneNumber, countryCode)`
3. **Tao address:** `user.AddAddress(type, recipientName, street, ward, district, city, postalCode, phoneNumber, isDefault, nowUtc)`
4. **Xu ly mac dinh:** Neu `isDefault = true`, tat ca dia chi khac duoc dat `isDefault = false`
5. **Luu:** `SaveChangesAsync`

### Set Default Address Handler

1. **Tim address** theo `addressId`, kiem tra thuoc user hien tai
2. **Dat mac dinh:** Tat ca dia chi khac cua user -> `isDefault = false`, dia chi nay -> `isDefault = true`
3. **Luu:** `SaveChangesAsync`

### Remove Address Handler

1. **Tim address** theo `addressId`, kiem tra thuoc user hien tai
2. **Xoa:** `user.RemoveAddress(addressId)`
3. **Luu:** `SaveChangesAsync`

## Luu y nghiep vu

- **Dia chi dau tien** duoc tu dong dat lam mac dinh
- **Cap nhat** chi cap nhat cac truong duoc gui (partial update)
- **Loai dia chi:** `Shipping` (giao hang), `Billing` (hoa don), `Both` (ca hai)
- **Phone number** trong dia chi co the khac voi so dien thoai chinh cua user (so nguoi nhan)
- Dia chi duoc dung trong luong **Shipping** khi dat hang va gui hang dau gia
- He thong su dung dia chi theo phan cap Viet Nam: `City > District > Ward > Street`
