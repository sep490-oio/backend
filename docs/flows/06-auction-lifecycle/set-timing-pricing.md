# Set Timing & Pricing

## Tong quan

Sau khi tao Auction (trang thai `Draft` hoac `Approved`), seller can thiet lap lich trinh (timing) va co the cap nhat cau hinh gia (pricing). Co hai endpoint:
1. `PUT api/auctions/{auctionId}/timing` - Dat lich cho Auction da duoc Approved
2. `PUT api/auctions/{auctionId}` - Cap nhat toan bo cau hinh (pricing + timing + auctionType)

## Actors

- **Seller (Nguoi ban):** Thiet lap lich va cau hinh gia

## Endpoint Sequence

### Step 1: Dat lich (SetTiming)

- **Method:** `PUT api/auctions/{auctionId}/timing`
- **Auth:** Required (Seller, owner cua Item)
- **Request:**
  ```json
  {
    "startTime": "2026-04-01T10:00:00Z",
    "endTime": "2026-04-01T22:00:00Z",
    "qualificationStartAt": "2026-03-25T00:00:00Z",
    "qualificationEndAt": "2026-04-01T09:00:00Z",
    "autoExtend": true,
    "extensionMinutes": 5
  }
  ```
- **Response:** `200 OK` - `AuctionDto`
- **Ghi chu:**
  - Chi cho phep khi Auction o trang thai `Approved`
  - `qualificationStartAt` va `qualificationEndAt` phai o tuong lai
  - `extensionMinutes` phai tu 1-30
  - Sealed auction khong ho tro `autoExtend = true`
  - Sau khi set timing, Auction chuyen sang trang thai `Scheduled`
  - Raise `AuctionScheduledEvent` voi `StartTime` va `EndTime`

### Step 2: Cap nhat cau hinh (UpdateAuction)

- **Method:** `PUT api/auctions/{auctionId}`
- **Auth:** Required (Seller, owner cua Item)
- **Request:**
  ```json
  {
    "startingPrice": 100000,
    "bidIncrement": 10000,
    "reservePrice": 500000,
    "buyNowPrice": 1000000,
    "currency": "VND",
    "auctionType": "regular",
    "startTime": "2026-04-01T10:00:00Z",
    "endTime": "2026-04-01T22:00:00Z",
    "qualificationStartAt": "2026-03-25T00:00:00Z",
    "qualificationEndAt": "2026-04-01T09:00:00Z",
    "autoExtend": true,
    "extensionMinutes": 5
  }
  ```
- **Response:** `200 OK` - `AuctionDto`
- **Ghi chu:**
  - **Khong cho phep** khi Auction o trang thai: `Active`, `Ended`, `Sold`, `PaymentDefaulted`, `Failed`, `Cancelled`, `Terminated`
  - **Khong cho phep** khi da co bid (`BidCount > 0`)
  - Neu Auction dang o `Scheduled` ma khong cung cap timing moi -> loi `TimingRequired`
  - Neu Auction o `Approved` va co timing -> tu dong chuyen sang `Scheduled`
  - Tat ca cac field deu optional - chi cap nhat nhung field duoc gui
  - Neu timing thay doi va Auction o `Scheduled`, he thong tu dong reschedule Quartz job
  - `startTime` va `endTime` phai duoc cung cap cung luc

## Validation Rules

### AuctionInfo.Create
- `startTime` phai truoc `endTime`
- Thoi gian dau gia toi thieu duoc cau hinh trong `RuntimeSettings`

### QualificationWindow.Create
- `qualificationStartAt` phai truoc `qualificationEndAt`
- Qualification window phai ket thuc truoc hoac bang `startTime`

### AuctionPricing.Create
- `startingPrice >= 0`
- `bidIncrement >= 0`
- `reservePrice >= startingPrice` (neu co)
- `buyNowPrice >= startingPrice` (neu co)

## Domain Events

| Event                    | Khi nao                                    |
|--------------------------|--------------------------------------------|
| `AuctionScheduledEvent`  | SetTiming thanh cong (Approved -> Scheduled)|

## Side Effects

- Khi Auction chuyen sang `Scheduled` qua UpdateAuction, he thong goi `IAuctionScheduler.ScheduleStartAsync()` de len lich Quartz job

## Luu y nghiep vu

- Seller co the cap nhat pricing nhieu lan truoc khi co bid
- Mot khi da co bid, khong the thay doi cau hinh nua
- Timing bat buoc phai co `QualificationWindow` - day la khoang thoi gian nguoi mua co the dang ky tham gia (dat coc) truoc khi phien dau gia bat dau
