# Tao URL Thanh Toan VNPay

## Tong quan

Frontend goi endpoint nay de nhan `paymentUrl`, sau do redirect user sang trang thanh toan VNPay. He thong tao Transaction o trang thai `Pending`, generate URL voi cac tham so VNPay, va tra ve cho client.

## Actors

- **Buyer** (nguoi mua) - goi endpoint de thanh toan
- **System** - tao Transaction, generate URL

## Endpoint Sequence

### Step 1: Tao URL thanh toan

- **Method:** `POST /api/payments/vnpay/create-url`
- **Auth:** Required (Bearer JWT)
- **Request:**
  ```json
  {
    "amount": 500000,
    "currency": "VND",
    "purpose": "AuctionDeposit | OrderPayment | AuctionBuyNow | WalletTopUp",
    "description": "Mo ta giao dich",
    "bankCode": "VNBANK",
    "auctionId": "guid (bat buoc khi purpose = AuctionDeposit | AuctionBuyNow)",
    "orderId": "guid (bat buoc khi purpose = OrderPayment)",
    "buyNowReservationId": "guid (bat buoc khi purpose = AuctionBuyNow)",
    "paymentMethodId": "guid (tuy chon - dung token the da luu)",
    "saveCard": false
  }
  ```
- **Response:** `200 OK`
  ```json
  {
    "transactionId": "guid",
    "transactionRef": "20260319120000_abc123...",
    "paymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?..."
  }
  ```
- **Ghi chu:**
  - `purpose` phai nam trong tap: `AuctionDeposit`, `OrderPayment`, `AuctionBuyNow`, `WalletTopUp`
  - `currency` phai la `VND`
  - `amount` phai >= 0

## Business Logic Chi Tiet

### Validation theo Purpose

**AuctionDeposit:**
- `auctionId` bat buoc
- Auction phai ton tai va co trang thai hop le (khong phai Cancelled/Ended/Sold/Failed)
- Seller khong the dat coc cho chinh auction cua minh
- Auction phai co cau hinh Qualification Window
- Qualification Window phai dang mo (chua dong, chua het han)
- User chua co deposit dang Hold cho auction nay

**AuctionBuyNow:**
- `auctionId` va `buyNowReservationId` bat buoc

**OrderPayment:**
- `orderId` bat buoc (thong thuong su dung CheckoutOrder endpoint thay the)

### Idempotency Check

He thong kiem tra xem da co Transaction `Pending` nao cho cung Order/Auction/Reservation chua:
- Neu da co: tai su dung Transaction cu, khong tao moi
- Neu chua co: tao Transaction moi

### Route theo PaymentMethod

1. **Co `paymentMethodId`:** Su dung VNPay Token Pay (thanh toan bang the da luu)
2. **`saveCard = true`:** Su dung VNPay Pay and Create Token (thanh toan + luu token)
3. **Mac dinh:** Su dung VNPay Pay thong thuong

## Domain Events & Side Effects

- Transaction duoc tao voi trang thai `Pending`
- Khi callback thanh cong: `TransactionCompletedDomainEvent`
- Khi callback that bai: `TransactionFailedDomainEvent`

## Thanh Toan Qua Wallet (CheckoutOrder)

Ngoai VNPay, he thong con ho tro thanh toan qua Wallet thong qua endpoint `POST /api/payments/checkout`.

### Payment Method

Truong `paymentMethod` trong `CheckoutOrderCommand` ho tro 3 gia tri:

| Gia tri | Mo ta |
|---|---|
| `vnpay` | **(Mac dinh)** Thanh toan toan bo qua VNPay gateway |
| `wallet` | Thanh toan toan bo bang so du Wallet |
| `wallet_vnpay` | Hybrid: dung Wallet truoc, phan con lai thanh toan qua VNPay |

### Flow `wallet` (Thanh toan toan bo bang Wallet)

1. Lay Order va goi `InitializePayment()`
2. Lay Wallet cua buyer
3. Tinh `remainingAmount = orderAmount - depositAmount` (tru tien coc auction winner neu co)
4. Kiem tra `wallet.BalanceAmount >= remainingAmount`, neu khong du -> loi `Wallet.InsufficientBalance`
5. Tao Transaction (type = `Payment`), TransactionRef format: `WLT-{yyyyMMddHHmmss}_{Guid}` cat con 36 ky tu
6. Neu co winner deposit: `winnerDeposit.ConvertToPayment()` + `wallet.DebitPending()`
7. `wallet.Debit(remainingAmount)` tru so du
8. Tao Escrow cho order
9. `transaction.MarkAsCompleted()` + `order.MarkAsPaid()`
10. Response tra ve `paymentUrl = null` (khong can redirect)

### Flow `wallet_vnpay` (Hybrid Wallet + VNPay)

1. Tinh `walletPortion = min(walletBalance, remainingAmount)` va `vnpayPortion = remainingAmount - walletPortion`
2. Neu `vnpayPortion <= 0`: fallback ve flow `wallet` (Wallet du tien)
3. Neu `vnpayPortion > 0`:
   - `wallet.Hold(walletPortion)` - giu tam phan wallet
   - Tao VNPay URL cho `vnpayPortion` (chi phan chua du)
   - Neu tao URL that bai: `wallet.Unhold()` rollback
4. Response tra ve `paymentUrl` de frontend redirect sang VNPay cho phan con lai
5. Khi VNPay callback thanh cong: he thong se apply wallet hold + VNPay amount de hoan tat thanh toan

## Luu y nghiep vu

- TransactionRef co format: `{yyyyMMddHHmmss}_{Guid}` cat con 36 ky tu
- URL chi co hieu luc trong thoi gian VNPay quy dinh (thuong 15 phut)
- Mot Transaction Pending co the duoc tai su dung neu user quay lai thanh toan
- IP address cua user duoc gui kem de VNPay verify
- Voi `wallet` payment, response `paymentUrl` se la `null` vi khong can redirect
- Voi `wallet_vnpay`, deposit cua auction winner duoc tru truoc, sau do wallet balance, cuoi cung moi den VNPay
