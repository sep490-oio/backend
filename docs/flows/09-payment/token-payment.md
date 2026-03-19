# Thanh Toan Bang Token The (Token Payment)

## Tong quan

Cho phep user thanh toan nhanh bang the da lien ket truoc do (VNPay Token). He thong ho tro 3 flow: (1) Pay thuong, (2) Pay and Create Token (thanh toan + luu the), (3) Token Pay (dung token da luu). Ngoai ra co flow Link Card de chi lien ket the ma khong thanh toan.

## Actors

- **User** - nguoi dung da dang ky
- **VNPay** - cong thanh toan

## Endpoint Sequence

### Flow 1: Link Card (Chi lien ket the)

#### Step 1: Tao URL lien ket the
- **Method:** `POST /api/payments/methods/link-card`
- **Auth:** Required
- **Request:**
  ```json
  {
    "cardType": "ATM"
  }
  ```
- **Response:** `200 OK`
  ```json
  {
    "paymentUrl": "https://sandbox.vnpayment.vn/..."
  }
  ```
- **Ghi chu:** Redirect user sang VNPay de nhap thong tin the va OTP

#### Step 2: VNPay callback tao PaymentMethod
- Khi callback thanh cong voi `vnp_token`, he thong tu dong tao PaymentMethod

### Flow 2: Thanh toan bang the da luu

#### Step 1: Tao URL thanh toan voi token
- **Method:** `POST /api/payments/vnpay/create-url`
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
- **Response:** `200 OK` (tra ve paymentUrl dang token_pay)
- **Ghi chu:** He thong tu dong associate Transaction voi PaymentMethod

### Flow 3: Thanh toan lan dau + luu token

#### Step 1: Tao URL thanh toan voi saveCard=true
- **Method:** `POST /api/payments/vnpay/create-url`
- **Auth:** Required
- **Request:**
  ```json
  {
    "amount": 500000,
    "currency": "VND",
    "purpose": "WalletTopUp",
    "description": "Nap tien vi",
    "saveCard": true
  }
  ```
- **Response:** `200 OK` (tra ve paymentUrl dang pay_and_create)

## Quan ly Payment Methods

### Them phuong thuc thanh toan (thu cong)
- **Method:** `POST /api/payments/methods`
- **Auth:** Required
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
- **Response:** `200 OK` -> Guid (PaymentMethod ID)

### Lay danh sach phuong thuc
- **Method:** `GET /api/payments/methods`
- **Auth:** Required
- **Response:** `200 OK` -> `PaymentMethodDto[]`

### Xoa phuong thuc
- **Method:** `DELETE /api/payments/methods/{id}`
- **Auth:** Required
- **Response:** `204 No Content`

### Dat phuong thuc mac dinh
- **Method:** `POST /api/payments/methods/{id}/default`
- **Auth:** Required
- **Response:** `204 No Content`

## Auto-Create PaymentMethod tu VNPay Token

Khi callback VNPay chua `vnp_token` (tu flow pay_and_create hoac token_pay):
1. Kiem tra da ton tai PaymentMethod voi cung token chua
2. Neu co: update card info
3. Neu chua co: tao PaymentMethod moi tu token
4. Link Transaction voi PaymentMethod
5. Day la best-effort - khong fail toan bo payment neu loi

## Luu y nghiep vu

- PaymentMethod chi luu masked card number, khong luu so the day du
- VNPay Token co thoi han su dung tuy VNPay quy dinh
- Mot user co the co nhieu PaymentMethod nhung chi 1 default
- Chi ho tro `Type = VnPay` hien tai
