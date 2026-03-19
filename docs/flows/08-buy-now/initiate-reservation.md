# Initiate Buy-Now Reservation

## Tong quan

Khi nguoi mua muon mua ngay, he thong tao mot reservation voi thoi han 15 phut. Trong thoi gian nay, auction bi "lock" - khong cho phep bidding cho den khi reservation het han hoac duoc xu ly.

## Actors

- **Buyer (Nguoi mua):** Khoi tao buy-now

## Endpoint Sequence

### Cach 1: SignalR Hub

- **Hub:** `/hubs/auction`
- **Method:** `BuyNow(auctionId)`
- **Auth:** Required (Permission: `Auctions.BuyNow`)
- **Response:** `HubCommandResult<BuyNowCheckoutDto>`

### Cach 2: REST API

- **Method:** `POST api/auctions/{auctionId}/buy-now`
- **Auth:** Required (Permission: `Auctions.BuyNow`)
- **Request:** Khong co body
- **Response:** `200 OK` - `BuyNowCheckoutDto`
  ```json
  {
    "reservationId": "guid",
    "paymentUrl": "https://sandbox.vnpayment.vn/...",
    "expiresAt": "2026-04-01T10:15:00Z",
    "buyNowPrice": { "amount": 2000000, "currency": "VND" },
    "depositAppliedAmount": { "amount": 500000, "currency": "VND" },
    "amountDue": { "amount": 1500000, "currency": "VND" }
  }
  ```

## Business Logic (BuyNowCommand Handler)

```
BuyNowCommand
    |
    v
AuctionGrain.InitiateBuyNowReservationAsync(buyerId, 15 min window)
    |
    v
auction.InitiateBuyNowReservation(buyerId, nowUtc, reservationWindow)
    |
   [Thanh cong?]
    |           |
  [Co]       [Khong] -> return Error
    |
    v
CreateVnPayPaymentUrlCommand(amount: gatewayAmountDue, purpose: "auction_buy_now")
    |
   [Thanh cong?]
    |           |
  [Co]       [Khong]
    |           |
    |     AuctionGrain.FailBuyNowReservationAsync(reason: "payment_url_creation_failed")
    |           |
    |        return Error
    |
    v
AuctionGrain.AttachBuyNowPaymentAsync(reservationId, transactionId)
    |
   [Thanh cong?]
    |           |
  [Co]       [Khong]
    |           |
    |     AuctionGrain.FailBuyNowReservationAsync(reason: "payment_transaction_attach_failed")
    |           |
    |        return Error
    |
    v
Return BuyNowCheckoutDto voi paymentUrl
```

## Domain Logic (InitiateBuyNowReservation)

`auction.InitiateBuyNowReservation(buyerId, nowUtc, reservationWindow)`:

1. **EnsureCanInitiateBuyNow(buyerId, nowUtc):**
   - BuyNowPrice phai ton tai va kha dung
   - Khong co active reservation khac
   - Buyer khong phai seller
   - Status phai la `Scheduled`
   - Dang trong QualificationWindow

2. **EnsureBuyerQualifiedForBuyNow(buyerId, nowUtc):**
   - Neu chua co participant -> tu dong tao voi role "buy_now"
   - Neu da co nhung chua qualified -> tu dong qualify

3. **Tinh toan gia:**
   - `buyNowPrice` = Pricing.BuyNowPrice
   - `depositAmount` = held deposit cua buyer (neu co)
   - `appliedDepositAmount` = min(depositAmount, buyNowPrice)
   - `gatewayAmountDue` = buyNowPrice - appliedDepositAmount

4. **Tao reservation:**
   - `AuctionBuyNowReservation.Create()`
   - `expiresAt = nowUtc + 15 phut`
   - Status: `PendingPayment`

5. **Raise event:**
   - `AuctionBuyNowReservedEvent`

## Deposit Auto-Apply

Khi buyer da dat coc truoc do:
- He thong tu dong ap dung deposit vao gia buy-now
- `appliedDepositAmount = min(heldDeposit, buyNowPrice)`
- Buyer chi phai thanh toan chenh lech qua VNPay (`gatewayAmountDue`)

Vi du:
- BuyNowPrice: 2,000,000 VND
- Held Deposit: 500,000 VND
- Applied Deposit: 500,000 VND
- Gateway Amount Due: 1,500,000 VND

## SignalR Notifications

### BuyNowReserved (gui toi group `auction:{auctionId}`)
```json
{
  "auctionId": "guid",
  "reservationId": "guid",
  "buyerId": "guid",
  "buyNowPrice": 2000000,
  "depositAppliedAmount": 500000,
  "amountDue": 1500000,
  "expiresAt": "2026-04-01T10:15:00Z"
}
```

## Locking Effect

Khi reservation duoc tao:
- `auction.GetActiveBuyNowReservation(nowUtc)` tra ve reservation nay
- `EnsureNotLockedByBuyNowReservation()` block bidding
- `EndAuctionJob` skip neu con reservation active
- Chi co 1 reservation active tai mot thoi diem per auction

## Error Cases

| Error                                  | Khi nao                                      |
|----------------------------------------|----------------------------------------------|
| `Auction.NotSupportBuyNow`             | Auction khong co BuyNowPrice                 |
| `Auction.BuyNowReservationActive`      | Da co reservation khac dang active            |
| `Auction.SelfBid`                      | Buyer la seller                               |
| `Auction.BuyNowUnavailableForScheduledAuction` | Status khong phai Scheduled hoac QualificationWindow dong |

## Luu y nghiep vu

- Reservation window la 15 phut (hardcoded trong `BuyNowCommandHandler`)
- Sau khi tao reservation, buyer duoc redirect den VNPay de thanh toan
- Neu tao VNPay URL that bai, reservation tu dong bi fail
- Neu attach payment transaction that bai, reservation tu dong bi fail
- Buyer duoc tu dong qualify khi initiate buy-now (khong can dat coc rieng)
- Deposit da dat duoc tu dong khau tru khoi gia mua
