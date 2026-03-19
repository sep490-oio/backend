# Buy-Now Payment (Thanh toan qua VNPay)

## Tong quan

Sau khi tao reservation, buyer duoc redirect den VNPay de thanh toan so tien con lai (sau khi tru deposit). He thong su dung VNPay payment gateway voi IPN (Instant Payment Notification) callback de xu ly ket qua.

## Actors

- **Buyer (Nguoi mua):** Thanh toan tren giao dien VNPay
- **VNPay:** Payment gateway
- **System:** Xu ly callback va cap nhat trang thai

## Endpoint Sequence

### Step 1: Buyer thanh toan tren VNPay

- Buyer duoc redirect den `paymentUrl` tu buoc Initiate Reservation
- Buyer nhap thong tin the/tai khoan va xac nhan thanh toan
- VNPay xu ly giao dich

### Step 2: VNPay IPN Callback

- **Method:** `GET api/payments/vnpay/ipn`
- **Auth:** Anonymous (server-to-server tu VNPay)
- **Xu ly:**
  1. Xac thuc signature (checksum) tu VNPay
  2. Tim `PaymentTransaction` trong DB theo `vnp_TxnRef`
  3. Kiem tra amount khop
  4. Cap nhat trang thai transaction

### Step 3: VNPay Return URL

- **Method:** `GET api/payments/vnpay/return`
- **Auth:** Anonymous (redirect tu VNPay)
- **Xu ly:** Redirect buyer ve frontend voi thong tin ket qua

## Payment Flow

```
Buyer tren VNPay
        |
     [Thanh toan]
        |
   +----+----+
   |         |
[Success] [Failed]
   |         |
   v         v
VNPay IPN    VNPay IPN
(success)    (failed)
   |         |
   v         v
Update     Update
Transaction Transaction
(Paid)     (Failed)
   |         |
   v         v
Process    Fail
BuyNow     Reservation
Payment
   |
   v
Finalize
Reservation
(xem finalize.md)
```

## VNPay Payment Parameters

Khi tao payment URL (tu BuyNowCommand):

| Parameter    | Gia tri                                            |
|--------------|----------------------------------------------------|
| `Amount`     | `reservation.GatewayAmountDue.Amount`              |
| `Currency`   | `reservation.GatewayAmountDue.Currency`            |
| `Purpose`    | `auction_buy_now`                                  |
| `IpAddress`  | IP cua buyer                                       |
| `Description`| `"AuctionBuyNow - Auction #{auctionId}"`           |
| `AuctionId`  | ID cua phien dau gia                               |
| `BuyNowReservationId` | ID cua reservation                        |

## Luu y nghiep vu

- VNPay IPN la server-to-server callback - khong phu thuoc vao browser cua buyer
- Return URL chi de redirect buyer ve frontend - khong dung de xu ly business logic
- Amount phai khop chinh xac giua request va callback
- He thong ho tro idempotent - neu IPN gui nhieu lan, chi xu ly lan dau
- Transaction co cac trang thai: Pending -> Paid/Failed
- Khi payment thanh cong cho buy-now, he thong tu dong finalize reservation (xem [finalize.md](./finalize.md))
- Khi payment that bai, reservation bi fail (xem [late-payment.md](./late-payment.md) cho truong hop dac biet)
