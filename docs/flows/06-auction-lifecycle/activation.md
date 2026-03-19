# Auction Activation

## Tong quan

Khi den thoi diem `StartTime`, he thong tu dong kich hoat phien dau gia tu `Scheduled` sang `Active`. Qua trinh nay bao gom kiem tra dieu kien tham gia (participant eligibility) va quyet dinh kich hoat hoac tu dong huy.

## Actors

- **System (Quartz Job):** `ActivateAuctionJob` duoc trigger theo lich
- **AuctionActivationService:** Service xu ly logic kich hoat

## Sequence

### Step 1: ActivateAuctionJob (Quartz)

- **Trigger:** Quartz trigger tai thoi diem `StartTime` (duoc len lich boi `QuartzAuctionScheduler.ScheduleStartAsync()`)
- **Job Key:** `activate-{auctionId}` trong group `AuctionLifecycleGroup`
- **Xu ly:**
  1. Lay `AuctionId` tu JobDataMap
  2. Send `ActivateAuctionCommand(auctionId)` qua MediatR

### Step 2: ActivateAuctionCommand Handler

- **Preconditions:**
  - Auction phai ton tai
  - Auction phai co `AuctionInfo` (timing)
  - Auction phai co `QualificationWindow`
  - Auction phai o trang thai `Scheduled` (idempotent: neu da Active thi return Success)

- **Goi `AuctionActivationService.ActivateScheduledAuctionAsync()`**

### Step 3: AuctionActivationService Logic

```
Load Auction (voi Deposits, Participants)
      |
      v
[Status == Scheduled?]
      |
   [Co bid-eligible participants?]
      |               |
    [Co]           [Khong]
      |               |
  Start()       CancelAuction()
      |          (reason: "no bid-eligible participants")
      |               |
  Save DB        ReturnItemToActive()
      |               |
  ScheduleEnd    CancelScheduler
  (EndTime)      (cancel ca Start va End jobs)
      |
   Active
```

### Kiem tra Participant Eligibility

`auction.HasBidEligibleParticipants(nowUtc)` kiem tra:
- Co it nhat mot `AuctionParticipant` dat dieu kien `IsBidEligibleParticipant(participant, nowUtc)`
- Participant phai:
  - Co `JoinStatus` khong phai `Withdrawn`
  - Da `IsQualified`
  - Co deposit hop le (held)

## Background Jobs

| Job                  | Schedule                  | Mo ta                              |
|----------------------|---------------------------|------------------------------------|
| `ActivateAuctionJob` | One-shot tai `StartTime`  | Kich hoat phien dau gia            |
| `AuctionPollingFallbackJob` | Recurring         | Safety net cho truong hop Quartz miss fire |

## Domain Events

| Event                  | Khi nao                                          |
|------------------------|--------------------------------------------------|
| `AuctionStartedEvent`  | Phien dau gia duoc kich hoat thanh cong           |
| `AuctionCancelledEvent`| Tu dong huy do khong co participant eligible       |

## Side Effects

- Khi kich hoat thanh cong: `IAuctionScheduler.ScheduleEndAsync(auctionId, endTime)` len lich `EndAuctionJob`
- Khi tu dong huy: `IAuctionScheduler.CancelAsync(auctionId)` huy tat ca Quartz jobs
- Item duoc tra ve trang thai `Active` khi auction bi auto-cancel

## Luu y nghiep vu

- Kich hoat la idempotent: neu Auction da o trang thai `Active`, handler return Success ma khong lam gi
- Qualification window phai dong truoc khi phien dau gia bat dau - nguoi mua phai dang ky (dat coc) trong khoang thoi gian nay
- Neu khong co participant nao du dieu kien tai thoi diem kich hoat, phien dau gia bi tu dong huy voi ly do cu the
- `QuartzAuctionScheduler` su dung `WithMisfireHandlingInstructionFireNow()` de dam bao job se chay ngay khi server khoi dong lai sau downtime
- Job co attribute `[DisallowConcurrentExecution]` de tranh chay song song
