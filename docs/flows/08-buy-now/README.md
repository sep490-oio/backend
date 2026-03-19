# 08 - Buy Now

## Tong quan

Buy Now cho phep nguoi mua mua ngay voi gia co dinh (BuyNowPrice) ma khong can dat gia. Quy trinh gom 3 buoc chinh:
1. **Reservation:** Nguoi mua yeu cau mua ngay, he thong tao reservation voi thoi han 15 phut
2. **Payment:** Nguoi mua thanh toan qua VNPay (tru di deposit da dat)
3. **Finalize:** Khi thanh toan thanh cong, he thong finalize reservation va ban auction

Buy Now chi kha dung khi phien dau gia o trang thai `Scheduled` va dang trong `QualificationWindow`.

## Actors

- **Buyer (Nguoi mua):** Mua ngay
- **System:** Xu ly reservation, payment callback, expiry
- **VNPay:** Payment gateway

## Flow Overview

```
Buyer yeu cau Buy Now
        |
        v
+--[Auction Scheduled + QualificationWindow open?]--+
|                                                     |
[Co]                                               [Khong]
|                                                     |
InitiateBuyNowReservation()                     Error
(lock 15 phut)
|
Tao VNPay Payment URL
|
Redirect buyer den VNPay
|
+----------[Buyer thanh toan]----------+
|                                       |
[Thanh cong]                     [That bai]
|                                       |
VNPay IPN callback               FailBuyNowReservation
|
FinalizeBuyNowReservation()
|
Auction -> Sold
Order duoc tao
```

## Dieu kien Buy Now

1. Auction phai co `BuyNowPrice` (Pricing.BuyNowAmount != null)
2. `Pricing.IsBuyNowAvailable == true`
3. Khong co active buy-now reservation khac
4. Buyer khong phai seller
5. Auction phai o trang thai `Scheduled`
6. Dang trong `QualificationWindow`

## Subflow Files

| File                                                | Mo ta                                           |
|-----------------------------------------------------|--------------------------------------------------|
| [initiate-reservation.md](./initiate-reservation.md)| Tao reservation va lock phien dau gia            |
| [payment.md](./payment.md)                          | Thanh toan qua VNPay                            |
| [finalize.md](./finalize.md)                        | Hoan tat mua ngay va tao order                  |
| [late-payment.md](./late-payment.md)                | Xu ly khi thanh toan thanh cong sau khi het han  |
| [reservation-expiry.md](./reservation-expiry.md)    | Tu dong het han reservation                      |

## SignalR Notifications

| Event                       | Khi nao                            |
|-----------------------------|-------------------------------------|
| `BuyNowReserved`            | Reservation duoc tao                |
| `BuyNowReservationReleased` | Reservation het han/that bai/cancel |
| `BuyNowExecuted`            | Mua ngay thanh cong                 |

## Reservation States

```
PendingPayment ──[Payment attached + Paid]──> Paid
PendingPayment ──[Het han]──> Expired
PendingPayment ──[That bai]──> Failed
```

## Locking Behavior

Khi co active buy-now reservation:
- **Bidding bi block:** `EnsureNotLockedByBuyNowReservation()` ngan tat ca bids
- **Auto-bid bi block:** Auto-bid khong the dat gia
- **EndAuctionJob bi skip:** Neu reservation con active, job return Success va doi expiry
- **Reservation chi co 1 tai mot thoi diem** per auction

## Luu y nghiep vu

- Buy Now chi kha dung khi Auction `Scheduled` va trong `QualificationWindow` - khong kha dung khi Active
- Deposit da dat duoc tu dong ap dung vao gia mua (giam so tien phai thanh toan qua VNPay)
- Reservation co thoi han 15 phut - sau do tu dong het han
- Khi buy-now thanh cong, tat ca bids dang hoat dong bi cancel va auction chuyen sang Sold
- Neu reservation het han nhung payment van thanh cong (late payment), tien duoc credit vao wallet
