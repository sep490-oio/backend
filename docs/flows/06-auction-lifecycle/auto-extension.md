# Auto Extension

## Tong quan

Khi co bid dat gan thoi diem ket thuc phien dau gia, he thong tu dong gia han thoi gian de dam bao nguoi mua co co hoi phan hoi. Day la tinh nang "anti-sniping" pho bien trong cac nen tang dau gia.

## Actors

- **System:** Tu dong gia han khi phat hien bid gan ket thuc

## Dieu kien Gia han

He thong goi `TryAutoExtend()` sau moi lan dat bid thanh cong (ca manual va auto-bid). Gia han chi xay ra khi:

1. Auction co `AuctionInfo` (timing)
2. `AuctionInfo.AutoExtend == true`
3. `ExtensionCount < maxExtensions` (gioi han so lan gia han, cau hinh trong `RuntimeSettings.Auction.MaxExtensionsPerAuction`)
4. Auction dang "sap ket thuc" (`IsEndingSoon`):
   - `Status == Active`
   - `RemainingTime(nowUtc) <= extensionThresholdMinutes` (cau hinh trong `RuntimeSettings.Auction.ExtensionThreshold`)

## Logic Gia han

```
PlaceBid() / PlaceAutoBidInternal()
    |
    v
TryAutoExtend(nowUtc, extensionThreshold, maxExtensions, maxDuration, triggerByBidId)
    |
    +--[khong co Info / AutoExtend = false / da dat maxExtensions / chua gan ket thuc]
    |      -> Skip (return Success)
    |
    +--[du dieu kien gia han]
           |
           v
    Info.Extend(maxDuration)
           |
           v
    NewEndTime = OldEndTime + ExtensionMinutes
    ExtensionCount++
           |
           v
    Raise AuctionExtendedEvent
           |
           v
    EndAuctionJob duoc reschedule boi event handler
```

## Reschedule EndAuctionJob

Khi `AuctionExtendedEvent` duoc raise:
- Domain event handler goi `IAuctionScheduler.RescheduleEndAsync(auctionId, newEndTime)`
- `QuartzAuctionScheduler.RescheduleEndAsync()`:
  1. Tim trigger hien tai cua `EndAuctionJob`
  2. Neu ton tai: reschedule voi `newEndTime`
  3. Neu khong ton tai: schedule moi

## Domain Events

| Event                    | Payload                                                        |
|--------------------------|----------------------------------------------------------------|
| `AuctionExtendedEvent`   | AuctionId, TriggerByBidId, PreviousEndTime, NewEndTime, ExtensionMinutes, ExtensionCount |

## SignalR Notifications

- `AuctionExtended(AuctionExtendedNotification)` gui toi tat ca clients trong group `auction:{auctionId}`:
  ```json
  {
    "auctionId": "guid",
    "newEndTime": "2026-04-01T22:05:00Z",
    "extensionMinutes": 5
  }
  ```

## Configuration (RuntimeSettings)

| Setting                                | Mo ta                                | Gia tri mac dinh |
|----------------------------------------|--------------------------------------|-------------------|
| `Auction.ExtensionThreshold`           | Khoang thoi gian "gan ket thuc"      | TimeSpan          |
| `Auction.MaxExtensionsPerAuction`      | So lan gia han toi da                | int               |
| `Auction.MaxDuration`                  | Thoi luong toi da cua phien dau gia  | TimeSpan          |

## Luu y nghiep vu

- Sealed auction **khong ho tro** auto-extension (kiem tra khi SetTiming va UpdateAuction)
- Moi lan gia han them `ExtensionMinutes` phut (cau hinh khi tao auction, 1-30 phut)
- Tong thoi luong phien dau gia khong vuot qua `MaxDuration`
- Gia han chi xay ra cho bid thanh cong (bid that bai khong trigger gia han)
- ExtensionCount duoc luu lai de tracking va gioi han
