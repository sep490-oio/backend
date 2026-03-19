# Reservation Expiry

## Tong quan

`ExpireBuyNowReservationsJob` la BackgroundService chay moi 1 phut, quet va danh dau cac buy-now reservations da het han. Sau khi expire reservation, neu phien dau gia da qua `EndTime`, job cung trigger EndAuction.

## Actors

- **System (BackgroundService):** Tu dong quet va expire reservations

## Background Job

### ExpireBuyNowReservationsJob

- **Type:** `BackgroundService` (khong phai Quartz - chay lien tuc)
- **Interval:** 1 phut (`TimeSpan.FromMinutes(1)`)
- **Batch size:** 50 auctions per cycle

## Logic

```
[Loop moi 1 phut]
        |
        v
Query auctions co reservations:
  - Status = PendingPayment
  - ExpiresAt <= nowUtc
  - Take(50)
        |
        v
[Duyet tung auction]
        |
   [Duyet tung expired reservation]
        |
   auction.ExpireBuyNowReservation(reservationId, nowUtc)
        |
   [reservation.IsPendingPayment?]
        |           |
      [Co]       [Khong] -> skip
        |
   reservation.Expire(nowUtc)
        |
   Raise AuctionBuyNowReservationReleasedEvent
        |
   SaveChanges
        |
   [Auction Active + EndTime <= nowUtc + No active reservation?]
        |              |
      [Co]          [Khong]
        |              |
   EndAuctionCommand  (done)
```

## Domain Logic

### ExpireBuyNowReservation

`auction.ExpireBuyNowReservation(reservationId, nowUtc)`:

1. Tim reservation theo ID
2. Neu `IsPendingPayment == false` -> skip (idempotent)
3. `reservation.Expire(nowUtc)` -> Status = `Expired`
4. Cap nhat `ModifiedAt`
5. Raise `AuctionBuyNowReservationReleasedEvent` (reason: "expired")

### Trigger EndAuction

Sau khi expire reservations, job kiem tra:
- Auction o trang thai `Active`
- `EndTime` da qua
- Khong con active reservation nao

Neu tat ca dieu kien thoa man -> send `EndAuctionCommand` de ket thuc phien dau gia.

Day la truong hop `EndAuctionJob` da chay nhung skip vi con reservation active. Sau khi reservation het han, `ExpireBuyNowReservationsJob` trigger lai EndAuction.

## SignalR Notifications

### BuyNowReservationReleased (gui toi group `auction:{auctionId}`)
```json
{
  "auctionId": "guid",
  "reservationId": "guid",
  "buyerId": "guid",
  "reason": "expired",
  "releasedAt": "2026-04-01T10:15:00Z"
}
```

## Domain Events

| Event                                  | Payload                                          |
|----------------------------------------|--------------------------------------------------|
| `AuctionBuyNowReservationReleasedEvent`| AuctionId, ReservationId, BuyerId, Reason        |

## Luu y nghiep vu

- Job chay moi 1 phut - do tre toi da khoang 1 phut tu luc reservation het han
- Batch size 50 de tranh overload DB khi co nhieu reservations het han cung luc
- Job la idempotent - neu reservation da expired thi skip
- Sau khi expire, auction unlock -> bidding duoc phep tro lai
- Quan trong: job kiem tra va trigger EndAuction neu can - dam bao phien dau gia khong bi treo
- BackgroundService chay lien tuc (khong phai Quartz) - don gian va reliable cho recurring task
- Errors duoc catch va log - job khong dung khi gap loi, tiep tuc o iteration tiep theo
