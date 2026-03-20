# 04 - Late Payment

## Scenario

A late payment occurs when a VNPay payment completes **successfully** but the buy-now reservation has already **expired** (`ExpiresAt <= now`). This can happen due to network delays, slow bank processing, or the buyer taking too long on the VNPay payment page.

The system detects this in `HandleAuctionBuyNowAsync` via:

```csharp
if (!reservation.IsActive(now))
```

Where `IsActive` is defined as:

```csharp
public bool IsActive(DateTime nowUtc) =>
    Status == BuyNowReservationStatus.PendingPayment &&
    ExpiresAt > nowUtc;
```

---

## What Happens

### Step 1: Credit Full Amount to Buyer Wallet

`CreditLateBuyNowPaymentToWalletAsync(transaction, now, ct)`:

- Loads the buyer's wallet by `transaction.UserId`.
- If `transaction.Amount.Amount > 0`, credits the full amount:

```csharp
wallet.Credit(
    amount: transaction.Amount.Amount,
    transactionId: transaction.Id,
    description: $"Late buy-now payment credited to wallet - TxnRef: {transaction.TransactionNumber.Value}",
    nowUtc: now);
```

- If `transaction.Amount.Amount <= 0`, the method returns success immediately (nothing to credit).
- If the wallet is not found, returns `Error.NotFound("Wallet.NotFound", ...)`.

### Step 2: Fail the Reservation

```csharp
auction.FailBuyNowReservation(
    reservation.Id,
    "late_payment_success",
    now);
```

This calls `reservation.Fail("late_payment_success", now)` which:
- Sets `Status = BuyNowReservationStatus.Failed`
- Sets `FailureReason = "late_payment_success"`
- Sets `ReleasedAt = now`
- Raises `AuctionBuyNowReservationReleasedEvent` with `Reason = "late_payment_success"`

### Step 3: No Order Created

The method returns `UnitResult.Success<Error>()` without creating any order, escrow, or further mutations.

### Step 4: Transaction Still Marked as Completed

After `HandleAuctionBuyNowAsync` returns success, the outer `HandleSuccessfulCallbackAsync` method:
- Calls `transaction.MarkAsCompleted(gatewayInfo, now)` -- the VNPay payment was indeed successful.
- Calls `SaveChangesAsync` -- persists wallet credit, reservation failure, and transaction completion.

### Step 5: Reservation Status = Failed

The reservation's final state:
- `Status`: `Failed`
- `FailureReason`: `"late_payment_success"`
- `ReleasedAt`: set to current time
- `OrderId`: null (no order was created)

### Step 6: Auction Remains Unchanged

The auction stays in whatever state it was in:
- If still `Scheduled` or `Active`: other buyers can bid or initiate new buy-now reservations.
- If already `Ended`, `Sold`, or `Failed`: the late payment has no effect on auction state.

The `ExpireBuyNowReservationsJob` may have already expired the reservation (changing status to `Expired`). In that case, the `FailBuyNowReservation` call checks `IsPendingPayment` first -- if the reservation is already `Expired`, it returns success without further changes, and the wallet credit still occurs.

### Step 7: Buyer Retains Wallet Funds

The credited amount is available in the buyer's wallet balance for:
- Future auction deposits
- Future buy-now purchases (if wallet payment is supported)
- Withdrawal (if implemented)

---

## Why This Design

The late payment path uses wallet credit instead of a VNPay refund for several reasons:

1. **Prevents double-sell**: The auction may have already ended or been sold to another buyer. Creating an order would conflict with the existing sale.
2. **Atomic safety**: The reservation's 15-minute window is a strict contract. Once expired, the system cannot guarantee the auction is still available.
3. **Simplicity**: VNPay refund processing is asynchronous and can take days. A wallet credit is immediate and deterministic.
4. **No lost funds**: The buyer's money is preserved in the platform wallet, not lost.

---

## Edge Cases

| Situation | Outcome |
|-----------|---------|
| Reservation already expired by background job (status = `Expired`) | `FailBuyNowReservation` finds `!IsPendingPayment`, returns success. Wallet credit still happens. |
| Reservation failed for another reason (status = `Failed`) | Same as above -- `!IsPendingPayment` guard prevents double-fail. Wallet credit still happens. |
| Transaction amount is 0 (fully covered by deposit) | `CreditLateBuyNowPaymentToWalletAsync` returns immediately (nothing to credit). Reservation still failed. |
| Wallet not found | Returns `Error.NotFound("Wallet.NotFound", ...)`. Transaction is NOT marked as completed. |
