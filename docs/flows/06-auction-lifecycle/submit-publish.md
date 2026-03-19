# Submit & Publish

## Tong quan

Sau khi cau hinh xong, Auction phai trai qua hai buoc de len lich:
1. **Submit** (`POST api/auctions/{auctionId}/submit`): Seller xac nhan cau hinh hoan tat
2. **Publish** (`POST api/auctions/{auctionId}/publish`): Seller xac nhan cong khai va len lich phien dau gia

Tuy thuoc vao viec Auction co timing hay khong, flow chuyen trang thai se khac nhau.

## Actors

- **Seller (Nguoi ban):** Submit va publish

## Endpoint Sequence

### Step 1: Submit Configuration

- **Method:** `POST api/auctions/{auctionId}/submit`
- **Auth:** Required (Seller, owner cua Item)
- **Request:** Khong co body
- **Response:** `204 No Content`
- **Ghi chu:**
  - Auction phai o trang thai `Draft`
  - Item phai o trang thai `Approved` (da duoc admin duyet)
  - Neu Auction **co AuctionInfo (timing)**: Draft -> `Scheduled` + raise `AuctionScheduledEvent`
  - Neu Auction **khong co AuctionInfo**: Draft -> `Approved`
  - Raise `AuctionSubmittedEvent` trong ca hai truong hop

### Step 2: Publish Auction

- **Method:** `POST api/auctions/{auctionId}/publish`
- **Auth:** Required (Seller, owner cua Item)
- **Request:** Khong co body
- **Response:** `204 No Content`
- **Ghi chu:**
  - Auction phai o trang thai `Scheduled`
  - Auction phai co `AuctionInfo` (timing bat buoc)
  - Neu `StartTime` da qua (phien dau gia le ra da bat dau):
    - Goi `AuctionActivationService.ActivateScheduledAuctionAsync()` ngay lap tuc
    - Neu khong co participant eligible -> auto cancel
    - Neu co participant eligible -> chuyen sang `Active` va schedule EndAuctionJob
  - Neu `StartTime` chua den:
    - Save thay doi
    - Goi `IAuctionScheduler.ScheduleStartAsync()` de len lich `ActivateAuctionJob`

## Flow Diagram

```
Draft ──[Submit]──> Scheduled ──[Publish]──> ActivateAuctionJob scheduled
                        |                           |
                 (co AuctionInfo)            (StartTime da qua?)
                                                    |
                                             +------+------+
                                             |             |
                                           [Chua]       [Da qua]
                                             |             |
                                    Schedule Quartz   Activate ngay
                                       Job               |
                                                  +------+------+
                                                  |             |
                                           [Co eligible   [Khong co]
                                            participants]      |
                                                  |       Auto Cancel
                                                  |
                                               Active

Draft ──[Submit]──> Approved ──[SetTiming]──> Scheduled ──[Publish]──> ...
                (khong co AuctionInfo)
```

## Domain Events

| Event                    | Khi nao                                    |
|--------------------------|--------------------------------------------|
| `AuctionSubmittedEvent`  | Submit thanh cong                           |
| `AuctionScheduledEvent`  | Submit voi Info hoac SetTiming thanh cong   |
| `AuctionStartedEvent`    | Publish + StartTime da qua + co participants|
| `AuctionCancelledEvent`  | Publish + StartTime da qua + khong co participants|

## Luu y nghiep vu

- Submit va Publish la hai buoc rieng biet - cho phep seller review lai cau hinh truoc khi cong khai
- Item phai o trang thai `Approved` truoc khi co the Submit Auction
- Neu Auction duoc Submit ma chua co timing, seller phai SetTiming rieng roi moi Publish duoc
- Publish la hanh dong cuoi cung cua seller - sau do he thong tu dong quan ly lifecycle
- AuctionSubmittedEvent chua truong `VerifyByPlatform` - cho phep platform kiem duyet phien dau gia truoc khi bat dau
