# Thanh Toan Bang Token The (VNPay Token Payment)

## Tong quan

He thong ho tro thanh toan nhanh bang the da lien ket (VNPay Token) voi 3 flow chinh va 1 flow xoa token:

```
                          +---------------------------+
                          |    User chon thanh toan    |
                          +---------------------------+
                                      |
                     +----------------+----------------+
                     |                |                 |
                     v                v                 v
            [Flow 1: Link Card] [Flow 2: Pay & Save] [Flow 3: Token Pay]
            token_create         pay_and_create        token_pay
            Chi lien ket the     Thanh toan + luu the  Dung token da luu
            Khong mat tien       Vua pay vua luu       Chi OTP, khong nhap the
                     |                |                 |
                     v                v                 v
               VNPay redirect    VNPay redirect    VNPay redirect (OTP only)
                     |                |                 |
                     v                v                 v
               Callback ->       Callback ->        Callback ->
               Tao PaymentMethod Completed + Token  Completed
```

## Actors

- **User** - nguoi dung da dang ky va xac thuc
- **VNPay** - cong thanh toan (sandbox/production)
- **System** - backend OIO xu ly callback va auto-create PaymentMethod

---

## Flow 1: Link Card (token_create) — Chi lien ket the, khong thanh toan

### Muc dich

User muon luu the truoc de su dung cho cac lan thanh toan sau. Khong phat sinh giao dich tien.

### Step 1: Tao URL lien ket the

- **Method:** `POST`
- **Route:** `api/payments/methods/link-card`
- **Auth:** Required (Bearer Token)
- **Request:**
  ```json
  {
    "cardType": "ATM"        // optional, VD: "ATM", "VISA", "JCB"
  }
  ```
- **Response:** `200 OK`
  ```json
  {
    "redirectUrl": "https://sandbox.vnpayment.vn/token_ui/create-token.html?vnp_Version=...&vnp_SecureHash=...",
    "transactionRef": "LINK-20260320143022-abcdef1234567890ab"
  }
  ```

### VNPay params duoc gui

| Param | Gia tri | Ghi chu |
|---|---|---|
| `vnp_Command` | `token_create` | Chi tao token, khong thanh toan |
| `vnp_TmnCode` | Config | Ma merchant |
| `vnp_TxnRef` | `LINK-{yyyyMMddHHmmss}-{GUID}` (max 36 ky tu) | Unique ref |
| `vnp_OrderInfo` | `"Lien ket the {txnRef}"` | Mo ta |
| `vnp_AppUserId` | UserId (GUID string) | Dinh danh user ben OIO |
| `vnp_CardType` | Optional | Loc loai the |
| `vnp_Locale` | `"vn"` | Ngon ngu |
| `vnp_ReturnUrl` | `{BeUrl}/api/payments/vnpay/return` | Callback URL |
| `vnp_CreateDate` | `yyyyMMddHHmmss` (GMT+7) | Thoi diem tao |
| `vnp_SecureHash` | HMAC-SHA512 | Chu ky bao mat |

> **Luu y:** Flow nay **khong gui** `vnp_Amount`, `vnp_ExpireDate`, `vnp_StoreToken`.

### Step 2: VNPay callback

Khi user hoan thanh nhap the va OTP tren VNPay, VNPay redirect ve `vnp_ReturnUrl` voi cac params:
- `vnp_ResponseCode` = `"00"` (thanh cong)
- `vnp_Token` / `vnp_token` — token da tao
- `vnp_CardNumber` / `vnp_card_number` — so the masked (VD: `"XXXXXXXXXXXX1234"`)
- `vnp_CardType` — loai the
- `vnp_BankCode` — ma ngan hang

He thong tu dong goi `TryLinkOrCreatePaymentMethodFromTokenAsync` de tao PaymentMethod moi tu token.

---

## Flow 2: Pay & Save (pay_and_create) — Thanh toan + Luu token

### Muc dich

User thanh toan mot giao dich va **dong thoi** luu the de su dung lan sau. Ket qua kep: giao dich completed + PaymentMethod duoc tao.

### Dieu kien kich hoat

Khi goi `POST api/payments/vnpay/create-url` voi:
- `saveCard = true`
- `paymentMethodId` = null (khong truyen)

### Step 1: Tao URL thanh toan + luu the

- **Method:** `POST`
- **Route:** `api/payments/vnpay/create-url`
- **Auth:** Required
- **Request:**
  ```json
  {
    "amount": 500000,
    "currency": "VND",
    "purpose": "WalletTopUp",
    "description": "Nap tien vi",
    "saveCard": true,
    "orderId": null,
    "auctionId": null,
    "paymentMethodId": null
  }
  ```
- **Response:** `200 OK`
  ```json
  {
    "transactionId": "guid",
    "transactionRef": "20260320143022_abcdef1234567890abcde",
    "paymentUrl": "https://sandbox.vnpayment.vn/token_ui/pay-create-token.html?..."
  }
  ```

### VNPay params duoc gui

| Param | Gia tri | Ghi chu |
|---|---|---|
| `vnp_Command` | `pay_and_create` | Thanh toan + tao token |
| `vnp_Amount` | `amount * 100` | VNPay yeu cau nhan 100 |
| `vnp_CurrCode` | `"VND"` | Chi ho tro VND |
| `vnp_TxnRef` | `{yyyyMMddHHmmss}_{GUID}` (max 36 ky tu) | Unique ref |
| `vnp_AppUserId` | UserId | Dinh danh user |
| `vnp_StoreToken` | `"1"` | Yeu cau VNPay luu token |
| `vnp_CardType` | Optional | Loc loai the |
| `vnp_ExpireDate` | CreateDate + 15 phut | URL het han |
| `vnp_ReturnUrl` | `{BeUrl}/api/payments/vnpay/return` | Callback URL |

### Xu ly callback

1. VNPay tra ve `vnp_ResponseCode = "00"` + `vnp_Token` + card info
2. Transaction duoc mark `Completed`
3. He thong auto-create PaymentMethod tu token (xem muc [Auto-create PaymentMethod](#auto-create-paymentmethod-tu-vnpay-token))
4. Transaction duoc link voi PaymentMethod moi

---

## Flow 3: Pay with Token (token_pay) — Thanh toan bang the da luu

### Muc dich

User thanh toan su dung token da luu truoc do. VNPay chi yeu cau xac thuc OTP (khong can nhap lai so the).

### Dieu kien kich hoat

Khi goi `POST api/payments/vnpay/create-url` voi:
- `paymentMethodId` = GUID cua PaymentMethod da luu
- `saveCard` = false (hoac khong truyen)

### Validation truoc khi tao URL

1. Tim PaymentMethod theo `paymentMethodId` + `userId`
2. Kiem tra `IsActive = true`
3. Kiem tra `Type = VnPay`
4. Kiem tra `VnPayToken` khong null/empty
5. Neu khong hop le → tra loi `PaymentMethod.NotFound` hoac `PaymentMethod.TokenRequired`
6. Associate Transaction voi PaymentMethod (`transaction.AssociatePaymentMethod(...)`)

### Step 1: Tao URL thanh toan voi token

- **Method:** `POST`
- **Route:** `api/payments/vnpay/create-url`
- **Auth:** Required
- **Request:**
  ```json
  {
    "amount": 500000,
    "currency": "VND",
    "purpose": "OrderPayment",
    "description": "Thanh toan don hang",
    "paymentMethodId": "guid-cua-payment-method",
    "orderId": "guid"
  }
  ```
- **Response:** `200 OK`
  ```json
  {
    "transactionId": "guid",
    "transactionRef": "20260320143022_abcdef1234567890abcde",
    "paymentUrl": "https://sandbox.vnpayment.vn/token_ui/payment-token.html?..."
  }
  ```

### VNPay params duoc gui

| Param | Gia tri | Ghi chu |
|---|---|---|
| `vnp_Command` | `token_pay` | Thanh toan bang token |
| `vnp_Amount` | `amount * 100` | VNPay yeu cau nhan 100 |
| `vnp_Token` | Token da luu tu PaymentMethod | Token cua user |
| `vnp_AppUserId` | UserId | Dinh danh user |
| `vnp_ExpireDate` | CreateDate + 15 phut | URL het han |
| `vnp_ReturnUrl` | `{BeUrl}/api/payments/vnpay/return` | Callback URL |

> **Luu y:** User chi can xac thuc OTP tren VNPay, khong can nhap lai thong tin the.

---

## Token Removal — Xoa token khi xoa PaymentMethod

Khi user xoa PaymentMethod co VNPay token, he thong:

1. Goi VNPay API `token_remove` (best-effort)
2. Deactivate PaymentMethod trong DB (`IsActive = false`, `IsDefault = false`)

### VNPay token_remove params

| Param | Gia tri |
|---|---|
| `vnp_Command` | `token_remove` |
| `vnp_TmnCode` | Config |
| `vnp_TxnRef` | `DEL-{yyyyMMddHHmmss}-{GUID}` (max 36 ky tu) |
| `vnp_AppUserId` | UserId |
| `vnp_Token` | Token can xoa |
| `vnp_OrderInfo` | Mo ta |
| `vnp_IpAddr` | `127.0.0.1` (server-side) |
| `vnp_CreateDate` | `yyyyMMddHHmmss` (GMT+7) |
| `vnp_SecureHash` | HMAC-SHA512 |

**API endpoint VNPay:** `POST {TokenRemoveUrl}` (default: `https://sandbox.vnpayment.vn/token_ui/remove-token.html`)

**Response:** JSON voi `vnp_response_code = "00"` la thanh cong.

> **Best-effort:** Neu VNPay API loi, he thong van deactivate PaymentMethod trong DB va log warning. Khong fail request cua user.

---

## Auto-create PaymentMethod tu VNPay Token

Logic nay chay trong `ProcessVnPayCallbackCommand` sau khi transaction duoc mark `Completed`.

### Flow xu ly

```
Callback chua vnp_Token?
       |
       +--> Khong --> Skip (flow pay thuong)
       |
       +--> Co --> Tim PaymentMethod co cung VnPayToken + UserId + IsActive
                    |
                    +--> Tim thay --> UpdateVnPayToken(newToken, maskedCard, cardType, bankCode)
                    |                 Associate Transaction voi PaymentMethod
                    |
                    +--> Khong thay --> Tao PaymentMethod moi (CreateFromVnPayToken)
                                        Insert vao DB
                                        Associate Transaction voi PaymentMethod
```

### Logic chi tiet

1. Kiem tra `callback.VnPayToken` co gia tri khong
2. Query: `PaymentMethod` where `UserId` + `Type = VnPay` + `VnPayToken` match + `IsActive`
3. **Neu da ton tai:** goi `UpdateVnPayToken()` de cap nhat card info moi nhat
4. **Neu chua ton tai:** goi `PaymentMethod.CreateFromVnPayToken()` voi `isDefault = false`
5. Goi `transaction.AssociatePaymentMethod(paymentMethod.Id)`
6. **Best-effort:** Toan bo logic wrap trong try-catch — neu loi chi log warning, khong fail payment

---

## PaymentMethod Entity

| Field | Type | Mo ta |
|---|---|---|
| `Id` | `PaymentMethodId` (Guid) | Primary key |
| `UserId` | `UserId` (Guid) | Chu so huu |
| `Type` | `PaymentMethodType` | Hien tai chi co `VnPay` |
| `Provider` | `string?` | `"vnpay"` |
| `Card` | `CardInfo` | Value object chua `LastFour`, `ExpiryMonth`, `ExpiryYear`, `HolderName` |
| `IsDefault` | `bool` | Phuong thuc mac dinh |
| `IsVerified` | `bool` | Da xac thuc (luon `true` khi tao tu VNPay) |
| `IsActive` | `bool` | Dang hoat dong (`false` = da xoa) |
| `TokenReference` | `string?` | = `VnPayToken` (dong bo) |
| `VnPayToken` | `string?` | Token tu VNPay callback |
| `MaskedCardNumber` | `string?` | So the an (VD: `XXXXXXXXXXXX1234`) |
| `VnPayCardType` | `string?` | Loai the VNPay tra ve (ATM, VISA, ...) |
| `BankCode` | `string?` | Ma ngan hang (NCB, VCB, ...) |
| `CreatedAt` | `DateTime` | Thoi diem tao |

---

## Bao mat va luu y

- **Khong luu so the day du:** He thong chi luu `MaskedCardNumber` (VD: `XXXXXXXXXXXX1234`) va `LastFour`
- **Token bao mat:** `VnPayToken` do VNPay cap va quan ly. He thong khong giai ma hay truy cap thong tin the tu token
- **HMAC-SHA512:** Moi request gui sang VNPay deu duoc ky bang `vnp_SecureHash` (HMAC-SHA512 voi `HashSecret`)
- **Signature validation:** Callback tu VNPay luon duoc verify signature truoc khi xu ly
- **Token co thoi han:** VNPay token co the het han tuy quy dinh cua VNPay
- **Mot user — nhieu PaymentMethod:** User co the co nhieu the lien ket nhung chi 1 the mac dinh (`IsDefault`)
- **Idempotency:** Khi tao URL thanh toan, he thong kiem tra transaction Pending da ton tai (theo Order/Auction) de tranh tao trung
- **URL het han:** Payment URL het han sau 15 phut (`vnp_ExpireDate`)
- **IP Address:** Flow link card va delete dung `127.0.0.1` (server-side, khong can IP user)
