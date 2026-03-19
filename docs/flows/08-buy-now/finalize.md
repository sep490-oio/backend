# Finalize Buy-Now Reservation

## Tong quan

Khi VNPay callback xac nhan thanh toan thanh cong, he thong finalize reservation: tao bid, chuyen auction sang Sold, va tao order cho buyer.

## Actors

- **System:** Tu dong xu ly khi nhan VNPay callback thanh cong

## Sequence

### Step 1: VNPay IPN thanh cong

- Payment transaction duoc cap nhat sang `Paid`
- He thong identify payment purpose la `auction_buy_now`

### Step 2: FinalizeBuyNowReservation (Domain Logic)

`auction.FinalizeBuyNowReservation(reservationId, nowUtc, ipAddress)`:

```
FindBuyNowReservation(reservationId)
        |
[reservation.IsPendingPayment?]
        |              |
      [Co]          [Khong] -> Error: InvalidState
        |
[reservation.IsActive(nowUtc)?]
        |              |
      [Co]          [Khong] -> Error: Expired
        |                   (xem late-payment.md)
        |
[Pricing.IsBuyNowAvailable?]
        |              |
      [Co]          [Khong] -> Error: NotSupportBuyNow
        |
Cancel tat ca existing bids (Active, Winning)
        |
Tao Bid.Create() voi BuyNowPrice
  -> MarkAsWon()
        |
Pricing.WithBuyNow()
  -> IsBuyNowAvailable = false
        |
reservation.MarkPaid(nowUtc)
        |
Set:
  - WinnerId = buyer
  - BidCount++
  - ActualEndTime = nowUtc
  - Status = Sold
        |
Tao AuctionPriceHistory (type: BuyNow)
        |
Raise AuctionSoldEvent
Raise BuyNowExecutedEvent
```

### Step 3: Tao Order

Sau khi finalize thanh cong:
1. He thong tao `Order` cho buyer va seller
2. Link order voi reservation: `auction.LinkBuyNowReservationOrder(reservationId, orderId, nowUtc)`
3. Reservation chuyen sang trang thai `Paid` voi OrderId

## Domain Events

| Event                 | Payload                                              |
|-----------------------|------------------------------------------------------|
| `AuctionSoldEvent`    | AuctionId, WinnerId, SellerId, FinalPrice, Currency, TotalBids |
| `BuyNowExecutedEvent` | AuctionId, BuyerId, Price, OccurredAt                |

## SignalR Notifications

### BuyNowExecuted (gui toi group `auction:{auctionId}`)
```json
{
  "auctionId": "guid",
  "buyerId": "guid",
  "price": 2000000
}
```

## State Changes

### Auction
- Status: `Scheduled` -> `Sold`
- WinnerId: set to buyer
- ActualEndTime: set to nowUtc
- BidCount: +1 (bid tu buy-now)
- Pricing: `IsBuyNowAvailable = false`

### BuyNowReservation
- Status: `PendingPayment` -> `Paid`
- OrderId: linked

### Existing Bids
- Tat ca bids Active/Winning bi cancel

### Auto-Bids
- `SyncAutoBidStateToWinner(buyerId)` cap nhat auto-bid states

## Luu y nghiep vu

- Finalize chi thanh cong khi reservation van active (chua het han 15 phut)
- Neu reservation da het han nhung payment van thanh cong -> xu ly late payment (xem [late-payment.md](./late-payment.md))
- Buy-now tao mot Bid entity thuc su (voi amount = BuyNowPrice) de nhat quan voi bidding system
- Sau khi finalize, BuyNow khong con kha dung (`IsBuyNowAvailable = false`)
- Tat ca bids cu bi cancel - ke ca bids tu qualified participants
- Order duoc tao va link voi reservation de tracking
- Auction chuyen thang tu Scheduled sang Sold (khong qua Active)
