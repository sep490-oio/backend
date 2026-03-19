# Cancel Auction

## Tong quan

Seller hoac Admin co the huy phien dau gia bat ky luc nao truoc khi ket thuc. Tat ca bids dang hoat dong se bi cancel, auto-bids bi terminate, va Item duoc tra ve trang thai Active.

## Actors

- **Seller (Nguoi ban):** Huy phien dau gia cua minh
- **Admin (role Catalogs.Admin):** Huy bat ky phien dau gia nao

## Endpoint Sequence

### Step 1: Cancel Auction

- **Method:** `POST api/auctions/{auctionId}/cancel`
- **Auth:** Required (Seller owner hoac Admin)
- **Request:**
  ```json
  {
    "reason": "Toi muon huy phien dau gia nay"
  }
  ```
- **Response:** `204 No Content`
- **Ghi chu:**
  - `reason` la bat buoc va khong duoc rong
  - Chi seller owner hoac admin role `Catalogs.Admin` moi co quyen huy

## Business Logic

```
Validate Auction exists
        |
Validate quyen (seller owner hoac admin)
        |
auction.CancelAuction(reason, nowUtc)
        |
   [CanTransitionTo(Cancelled)?]
        |           |
      [Co]       [Khong] -> Error: InvalidState
        |
  Status = Cancelled
  ActualEndTime = nowUtc
        |
  Cancel tat ca bids (Active, Winning)
        |
  TerminalizeAllAutoBids
        |
  Raise AuctionCancelledEvent
        |
  Item.ReturnToActive(nowUtc)
        |
  SaveChanges
        |
  IAuctionScheduler.CancelAsync(auctionId)
  (huy ActivateAuctionJob va EndAuctionJob)
```

## Trang thai cho phep Cancel

Tu `AuctionStatus.CanTransitionTo`:
- `draft` -> `cancelled`
- `pending` -> `cancelled`
- `approved` -> `cancelled`
- `scheduled` -> `cancelled`
- `active` -> `cancelled`

**Khong the cancel** khi o trang thai: `ended`, `sold`, `failed`, `payment_defaulted`, `terminated`

## Domain Events

| Event                  | Payload                      |
|------------------------|------------------------------|
| `AuctionCancelledEvent`| AuctionId, Reason, OccurredAt|

## SignalR Notifications

- `AuctionCancelled(AuctionCancelledNotification)` gui toi group `auction:{auctionId}`:
  ```json
  {
    "auctionId": "guid",
    "reason": "Toi muon huy phien dau gia nay"
  }
  ```

## Side Effects

- Tat ca Quartz jobs (ActivateAuction, EndAuction) duoc huy qua `IAuctionScheduler.CancelAsync()`
- Item duoc tra ve trang thai `Active` de co the dung cho phien dau gia khac
- Tat ca bids Active va Winning bi cancel
- Tat ca auto-bids bi terminate

## Luu y nghiep vu

- Cancel la hanh dong khong the hoan tac - trang thai `Cancelled` la terminal
- Khi cancel phien dau gia dang Active, tat ca bidders se mat bid cua ho
- Wallet holds tu auto-bid nen duoc xu ly boi side effects (event handlers)
- Item duoc tra ve Active de seller co the tao phien dau gia moi
