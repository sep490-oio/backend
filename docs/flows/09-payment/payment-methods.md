# Quan Ly Phuong Thuc Thanh Toan (Payment Methods)

## Tong quan

Module Payment Methods cho phep user quan ly cac phuong thuc thanh toan (the ngan hang) da lien ket voi tai khoan. Hien tai he thong chi ho tro `Type = VnPay`. PaymentMethod luu tru VNPay token de user thanh toan nhanh ma khong can nhap lai thong tin the.

PaymentMethod co the duoc tao tu 2 nguon:
1. **Tu dong (auto-create):** Khi VNPay callback tra ve `vnp_Token` (tu flow `token_create`, `pay_and_create`)
2. **Thu cong:** Qua endpoint `POST api/payments/methods` (it dung, chu yeu cho admin/testing)

---

## CRUD Endpoints

| Method | Route | Mo ta | Auth | Response |
|---|---|---|---|---|
| `POST` | `api/payments/methods` | Them phuong thuc thu cong | Required | `200 OK` → `Guid` (PaymentMethod ID) |
| `GET` | `api/payments/methods` | Lay danh sach phuong thuc cua user | Required | `200 OK` → `PaymentMethodDto[]` |
| `DELETE` | `api/payments/methods/{id}` | Xoa (deactivate) phuong thuc | Required | `204 No Content` |
| `POST` | `api/payments/methods/{id}/default` | Dat phuong thuc mac dinh | Required | `204 No Content` |
| `POST` | `api/payments/methods/link-card` | Tao URL VNPay de lien ket the | Required | `200 OK` → `{ redirectUrl, transactionRef }` |

---

## PaymentMethod Lifecycle

```
                    +------------------+
                    | Tao PaymentMethod |
                    +------------------+
                            |
               +------------+------------+
               |                         |
     [Auto-create tu VNPay]    [Them thu cong qua API]
     token_create callback      POST api/payments/methods
     pay_and_create callback
               |                         |
               v                         v
        +-------------+          +-------------+
        | IsActive = T |          | IsActive = T |
        | IsVerified=T |          | IsVerified=T |
        | IsDefault =F |          | IsDefault =? |
        +-------------+          +-------------+
               |                         |
               +------------+------------+
                            |
                            v
                   +------------------+
                   | Active & Verified |
                   +------------------+
                            |
               +------------+------------+
               |                         |
               v                         v
      [Set Default]              [Delete/Deactivate]
      POST .../default           DELETE .../{id}
               |                         |
               v                         v
        +-------------+          +----------------+
        | IsDefault =T |          | IsActive = F    |
        | (cac the khac|          | IsDefault = F   |
        |  bi bo default|         | + VNPay token   |
        |  IsDefault=F) |         |   remove (best  |
        +-------------+          |   effort)        |
                                 +----------------+
```

---

## Chi tiet tung Endpoint

### 1. Them phuong thuc thu cong

- **Route:** `POST api/payments/methods`
- **Request:**
  ```json
  {
    "type": "VnPay",
    "provider": "vnpay",
    "lastFour": "1234",
    "expiryMonth": 12,
    "expiryYear": 2027,
    "holderName": "NGUYEN VAN A",
    "tokenReference": "vnpay-token-xxx",
    "isDefault": true
  }
  ```
- **Response:** `200 OK` → `Guid` (ID cua PaymentMethod vua tao)
- **Ghi chu:** Endpoint nay chu yeu dung cho testing/admin. Trong production, PaymentMethod thuong duoc tao tu dong tu VNPay callback.

### 2. Lay danh sach phuong thuc

- **Route:** `GET api/payments/methods`
- **Response:** `200 OK`
  ```json
  [
    {
      "id": "guid",
      "type": "VnPay",
      "provider": "vnpay",
      "lastFour": "1234",
      "expiryMonth": null,
      "expiryYear": null,
      "holderName": null,
      "isDefault": true,
      "isActive": true,
      "createdAt": "2026-03-20T14:30:22Z",
      "maskedCardNumber": "XXXXXXXXXXXX1234",
      "vnPayCardType": "ATM",
      "bankCode": "NCB"
    }
  ]
  ```
- **Ghi chu:** Chi tra ve PaymentMethod cua user hien tai (filter theo `UserId`).

### 3. Xoa phuong thuc

- **Route:** `DELETE api/payments/methods/{id}`
- **Response:** `204 No Content`
- **Logic:**
  1. Tim PaymentMethod theo `id` + `userId`
  2. Neu khong tim thay → `404 NotFound`
  3. Neu da `IsActive = false` → return thanh cong (idempotent)
  4. Neu la VNPay token → goi VNPay API `token_remove` (best-effort, khong fail neu loi)
  5. Goi `Deactivate()`: set `IsActive = false`, `IsDefault = false`
  6. Luu DB

### 4. Dat phuong thuc mac dinh

- **Route:** `POST api/payments/methods/{id}/default`
- **Response:** `204 No Content`
- **Logic:**
  1. Tim PaymentMethod theo `id` + `userId`
  2. Bo `IsDefault` cua tat ca PaymentMethod khac cua user (`RemoveDefault()`)
  3. Set `IsDefault = true` cho PaymentMethod duoc chon (`SetDefault()`)
  4. Luu DB
- **Rang buoc:** Mot user chi co toi da 1 PaymentMethod mac dinh tai moi thoi diem.

### 5. Lien ket the qua VNPay (Link Card)

- **Route:** `POST api/payments/methods/link-card`
- **Request:**
  ```json
  {
    "cardType": "ATM"      // optional
  }
  ```
- **Response:** `200 OK`
  ```json
  {
    "redirectUrl": "https://sandbox.vnpayment.vn/token_ui/create-token.html?...",
    "transactionRef": "LINK-20260320143022-abcdef1234567890ab"
  }
  ```
- **Flow:** Xem chi tiet tai [token-payment.md — Flow 1: Link Card](./token-payment.md#flow-1-link-card-token_create--chi-lien-ket-the-khong-thanh-toan)

---

## Token Lifecycle

```
[Link Card / Pay & Save]
         |
         v
  VNPay cap vnp_Token
         |
         v
  Auto-create PaymentMethod
  (VnPayToken = vnp_Token)
         |
         v
  User su dung token_pay
  (chi xac thuc OTP)
         |
         v
  [Optional] Callback tra ve token moi
  → UpdateVnPayToken()
         |
         v
  User xoa PaymentMethod
         |
         v
  Goi VNPay token_remove (best-effort)
         |
         v
  Deactivate PaymentMethod
  (IsActive = false)
```

### Token duoc tao khi nao?

| Flow | VNPay Command | Ket qua |
|---|---|---|
| Link Card | `token_create` | Chi tao token, khong thanh toan |
| Pay & Save | `pay_and_create` | Thanh toan + tao token |
| Token Pay | `token_pay` | Su dung token da co (co the tra ve token moi trong callback) |

### Token duoc xoa khi nao?

- User goi `DELETE api/payments/methods/{id}`
- He thong goi VNPay `token_remove` API (best-effort)
- PaymentMethod bi deactivate trong DB

---

## PaymentMethod Entity Fields

| Field | Type | Mo ta |
|---|---|---|
| `Id` | `PaymentMethodId` (Guid) | Primary key |
| `UserId` | `UserId` (Guid) | Chu so huu |
| `Type` | `PaymentMethodType` | `VnPay` (hien tai chi ho tro 1 loai) |
| `Provider` | `string?` | `"vnpay"` |
| `Card` | `CardInfo` | Value object: `LastFour`, `ExpiryMonth`, `ExpiryYear`, `HolderName` |
| `IsDefault` | `bool` | Phuong thuc mac dinh? |
| `IsVerified` | `bool` | Da xac thuc (luon `true` khi tao tu VNPay) |
| `IsActive` | `bool` | Dang hoat dong (`false` khi bi xoa/deactivate) |
| `TokenReference` | `string?` | Dong bo voi `VnPayToken` |
| `VnPayToken` | `string?` | Token tu VNPay de thanh toan nhanh |
| `MaskedCardNumber` | `string?` | So the bi an (VD: `XXXXXXXXXXXX1234`) |
| `VnPayCardType` | `string?` | Loai the: ATM, VISA, JCB, ... |
| `BankCode` | `string?` | Ma ngan hang: NCB, VCB, TCB, ... |
| `CreatedAt` | `DateTime` | Thoi diem tao |

---

## VNPay-specific Fields

Cac field chi danh rieng cho VNPay token:

| Field | Nguon | Cap nhat khi |
|---|---|---|
| `VnPayToken` | `vnp_Token` / `vnp_token` tu callback | Auto-create hoac `UpdateVnPayToken()` |
| `MaskedCardNumber` | `vnp_CardNumber` / `vnp_card_number` tu callback | Auto-create hoac update |
| `VnPayCardType` | `vnp_CardType` tu callback | Auto-create hoac update |
| `BankCode` | `vnp_BankCode` tu callback | Auto-create hoac update |

> **Luu y case sensitivity:** VNPay co the tra ve `vnp_Token` hoac `vnp_token`, `vnp_CardNumber` hoac `vnp_card_number`. He thong xu ly ca 2 truong hop (xem `ProcessCallback` trong `VnPayGateway.cs`).

---

## Lien ket

- [Token Payment (chi tiet 3 flow)](./token-payment.md)
- [IPN Callback](./ipn-callback.md)
- [Tao URL thanh toan](./create-payment-url.md)
