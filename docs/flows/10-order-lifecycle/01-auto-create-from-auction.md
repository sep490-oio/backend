# 01 -- Auto-Create Order from Auction

Two paths lead to automatic order creation: Buy Now finalization and Auction Winner resolution.

## Path A -- Buy Now Winner

When a buyer completes a buy-now VNPay payment, the `ProcessVnPayCallbackCommand` handler routes to `HandleAuctionBuyNowAsync` (purpose = `AuctionBuyNow`).

### Flow

1. **Load reservation** -- `AuctionBuyNowReservation` is loaded with the auction, item, bids, deposits, and participants via `Include`.
2. **Check active** -- If `reservation.IsActive(now)` is false (expired), the payment is credited to the buyer's wallet as a late payment, and the reservation is failed with reason `"late_payment_success"`.
3. **CreateBuyNowOrder()** -- Private helper builds the order:
   - `shippingAddress` = buyer's default address (`IsDefault`) or first address
   - If no address exists, `ShippingSnapshot` uses placeholder: `"Address pending update"`
   - `OrderPricing`: `itemPrice = reservation.BuyNowPrice`, `shippingFee = 0`, `platformFee = 0`, `taxAmount = 0`, `totalAmount = buyNowPrice`
   - `Order.Create(...)` with `paymentDueAt = nowUtc` (immediate), notes = `"Created from buy-now payment callback."`
4. **FinalizeBuyNowReservation** -- `auction.FinalizeBuyNowReservation(reservationId, now)`
5. **LinkBuyNowReservationOrder** -- `auction.LinkBuyNowReservationOrder(reservationId, orderId, now)`
6. **Escrow (VNPay gateway amount)** -- `Escrow.Create(orderId, transactionId, transaction.Amount, currency, now)` -- only if `transaction.Amount.Amount > 0`
7. **Escrow (deposit portion)** -- If `reservation.DepositAppliedAmount.Amount > 0`:
   - Load the held `AuctionDeposit` for the buyer
   - `deposit.ConvertToPayment(now)` -- marks deposit as Held -> ConvertedToPayment
   - `wallet.DebitPending(depositAmount, ...)` -- deducts from wallet pending balance (fallback: `wallet.Debit()`)
   - Separate `Transaction` created: type = Payment, description prefix `[AuctionBuyNowDepositApplied]`
   - Second `Escrow.Create(orderId, fundingTxId, depositAppliedAmount, ...)`
8. **MarkAsPaid** -- `order.MarkAsPaid(now)` -- status transitions PendingPayment -> Paid immediately. No separate checkout needed.

### Failure Handling

If order creation fails, or FinalizeBuyNowReservation fails after payment:
- Payment is credited to buyer wallet (`CreditLateBuyNowPaymentToWalletAsync`)
- Reservation is failed with descriptive reason (`"order_creation_failed_after_payment"`, `"buy_now_finalize_failed_after_payment"`)

## Path B -- Auction Winner

When an auction ends with a winner, the `EndAuctionCommand` calls the auction grain's `EndAuctionAsync`. The grain calls `auction.Resolve(Sold)` which raises `AuctionSoldEvent`.

### AuctionSoldEventHandler Flow

1. **Load data** -- Loads auction (with watchers, item) and winner (with profile, addresses) using `AsNoTracking` + `Include`.
2. **EnsureOrderAsync** -- Checks if an order already exists for this auction + buyer. If not:
   - `Money.Create(finalPrice, currency)` for item price
   - `shippingAddress` = winner's default address or first address
   - `ShippingSnapshot` from address (or placeholder if no address)
   - `OrderPricing.Create(itemPrice, shippingFee=0, platformFee=0, taxAmount=0, totalAmount=itemPrice)`
   - `Order.Create(...)`:
     - `paymentDueAt = notification.OccurredAt.AddHours(48)` -- **48-hour payment deadline**
     - `notes` = `"Winner had no default address when order was generated."` if no address
   - Insert + SaveChanges
3. **Notifications** -- Creates notifications for:
   - Winner: `auction_won` (High priority) with checkout action payload
   - Seller: `auction_sold` (High priority)
   - Watchers: `auction_ended` (Normal priority)
4. **SignalR** -- `NotifyAuctionEndedAsync` broadcasts auction result to hub subscribers

### Key Difference from Path A

The auction winner order is created with status `PendingPayment` and requires the winner to call `POST /api/payments/checkout` within 48 hours. The winner's deposit is already held in their wallet from the qualification phase; it will be applied during checkout.

## OrderNumber Format

Generated in `Order.Create()`:

```
ORD-{yyyyMMddHHmmss}-{Guid:N}
```

Truncated to 31 characters via `[..31]`. Wrapped in `OrderNumber` value object.

## OrderPricing Value Object

| Field | Type | Description |
|-------|------|-------------|
| `ItemPrice` | `Money` | The final auction price or buy-now price |
| `ShippingFee` | `decimal` | Currently set to 0 at order creation |
| `PlatformFee` | `decimal` | Currently set to 0 at order creation |
| `TaxAmount` | `decimal` | Currently set to 0 at order creation |
| `TotalAmount` | `Money` | Sum of all components |

## ShippingSnapshot

Denormalized from the buyer's address at creation time:

| Field | Source |
|-------|--------|
| `RecipientName` | `shippingAddress.Recipient.RecipientName` or user display name |
| `Phone` | `shippingAddress.Recipient.Phone.Value` |
| `Address` | `shippingAddress.Address.Street` |
| `Ward` | `shippingAddress.Address.Ward` |
| `District` | `shippingAddress.Address.District` |
| `City` | `shippingAddress.Address.City` |

## Source Files

| File | Path |
|------|------|
| ProcessVnPayCallbackCommand | `src/core/OIO.Application/Context/PaymentContext/Commands/ProcessVnPayCallback/ProcessVnPayCallbackCommand.cs` |
| AuctionSoldEventHandler | `src/core/OIO.Application/Context/AuctionContext/EventHandlers/AuctionSoldEventHandler.cs` |
| Order (entity) | `src/core/OIO.Domain/Context/OrderContext/Aggregates/Orders/Order.cs` |
| EndAuctionCommand | `src/core/OIO.Application/Context/AuctionContext/Commands/EndAuction/EndAuctionCommand.cs` |
