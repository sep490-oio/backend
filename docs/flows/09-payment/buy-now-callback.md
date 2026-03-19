# Xu Ly Callback Mua Ngay (Buy Now)

## Tong quan

Khi user mua ngay (AuctionBuyNow) va thanh toan thanh cong qua VNPay, he thong kiem tra reservation con hieu luc, tao Order, tao Escrow, ap dung deposit (neu co), va danh dau don hang da thanh toan. Neu reservation het han, tien duoc hoan vao wallet.

## Actors

- **Buyer** - nguoi mua ngay
- **System** - xu ly callback, tao order, finalize reservation
- **VNPay** - gui ket qua thanh toan

## Preconditions

- Buyer da goi `POST /api/auctions/{auctionId}/buy-now` de tao BuyNowReservation
- Buyer da goi `POST /api/payments/vnpay/create-url` voi `purpose = AuctionBuyNow`

## Business Logic Chi Tiet (HandleAuctionBuyNowAsync)

### Buoc 1: Tim Reservation
- Tim `AuctionBuyNowReservation` theo `BuyNowReservationId` hoac `PaymentTransactionId`
- Load kem Auction, Item, Bids, Deposits, Participants

### Buoc 2: Kiem tra hieu luc Reservation
- `reservation.IsActive(now)` - reservation co con trong thoi han khong?
- Neu het han:
  - Hoan tien vao wallet: `wallet.Credit(amount, ...)`
  - Fail reservation: `auction.FailBuyNowReservation(reservationId, "late_payment_success", now)`
  - Ket thuc flow (tien da hoan, reservation bi huy)

### Buoc 3: Tao Order
- Lay thong tin buyer (profile, addresses)
- Tao `ShippingSnapshot` tu default address
- Tao `OrderPricing` voi `BuyNowPrice`
- `Order.Create(auctionId, buyerId, sellerId, shipping, pricing, ...)`

### Buoc 4: Finalize Reservation
- `auction.FinalizeBuyNowReservation(reservationId, now)`
- Neu that bai: hoan tien vao wallet va fail reservation

### Buoc 5: Link Order voi Reservation
- `auction.LinkBuyNowReservationOrder(reservationId, orderId, now)`

### Buoc 6: Tao Escrow tu VNPay payment
- `Escrow.Create(orderId, transactionId, amount, currency, now)`

### Buoc 7: Ap dung Deposit (neu co)
- Neu `reservation.DepositAppliedAmount > 0`:
  - Tim `AuctionDeposit` dang Hold cua buyer
  - Tao funding Transaction moi cho deposit applied
  - `deposit.ConvertToPayment(now)`
  - `wallet.DebitPending(depositAmount, ...)` hoac `wallet.Debit(...)`
  - Tao them Escrow cho phan deposit

### Buoc 8: Danh dau Order da thanh toan
- `order.MarkAsPaid(now)`

## Xu ly khi thanh toan that bai (HandleAuctionBuyNowFailedAsync)

- Tim reservation
- `auction.FailBuyNowReservation(reservationId, "payment_failed", now)`
- Reservation bi huy, auction tiep tuc binh thuong

## State Machine

```
Reservation: Active -> [Payment Success] -> Finalized -> Order Created -> Paid
Reservation: Active -> [Payment Failed] -> Failed
Reservation: Active -> [Late Payment] -> Failed (tien hoan vao wallet)
```

## Domain Events & Side Effects

- `TransactionCompletedDomainEvent` -> Audit log
- `WalletCreditedDomainEvent` (khi hoan tien late payment)
- Order duoc tao tu dong khi buy-now thanh cong
- Auction chuyen sang Sold khi finalize thanh cong

## Luu y nghiep vu

- BuyNowReservation co thoi han (thuong 15 phut) - het han thi tien hoan vao vi
- Deposit da dat truoc co the duoc ap dung vao thanh toan buy-now
- Khi buy-now thanh cong, auction ket thuc ngay lap tuc
- Neu tao order that bai sau khi da thanh toan, tien van duoc hoan vao wallet
