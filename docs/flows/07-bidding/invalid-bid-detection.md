# Invalid Bid Detection (Giam sat bid bat thuong)

## Tong quan

He thong tu dong giam sat cac lan dat gia that bai (invalid bid attempts) de phat hien hanh vi bat thuong nhu bot attacks, bid manipulation, hoac su co ky thuat. Khi vuot nguong, he thong tao `MonitoringAlert` de admin xu ly.

## Actors

- **System:** Tu dong ghi nhan va giam sat
- **Admin:** Xem, acknowledge, va resolve monitoring alerts

## Quy trinh Giam sat

### Khi Bid That bai

`PlaceBidCommandHandler.TrackInvalidBidAttemptAsync()`:

```
Bid that bai (validation error)
        |
        v
Ghi AuditLog:
  - action: "invalid_bid_attempt"
  - entityType: "Auction"
  - entityId: auctionId
  - payload: { bidderId, ipAddress, amount, currency, errorCode, errorMessage }
        |
        v
[InvalidBidBurstThreshold > 0?]
        |              |
      [Co]          [Khong]
        |              |
  Count recent      SaveChanges
  attempts             (done)
  (10 phut)
        |
  [recentAttempts >= threshold?]
        |              |
      [Co]          [Khong]
        |              |
  [Da co open alert?]  SaveChanges
        |        |        (done)
      [Co]    [Khong]
        |        |
  (skip)     Tao MonitoringAlert:
                type: "invalid_bid_burst"
                severity: High
                payload: {
                  auctionId,
                  bidderId,
                  ipAddress,
                  recentAttempts,
                  threshold,
                  windowMinutes: 10,
                  lastErrorCode
                }
```

### Tinh Recent Attempts

- **Window:** 10 phut (`InvalidBidWindow = TimeSpan.FromMinutes(10)`)
- **Dieu kien:** Cung auctionId AND (cung userId HOAC cung IP address)
- **Nguon:** `AuditLog` table voi `action == "invalid_bid_attempt"`

### Kiem tra Open Alert Trung lap

Truoc khi tao alert moi, he thong kiem tra:
- Co `MonitoringAlert` nao dang `Open` cho cung auction, type `invalid_bid_burst`
- Trong khoang 10 phut gan nhat
- Payload chua `bidderId` hoac `ipAddress` tuong tu

Neu da co -> khong tao alert trung lap

## Admin Endpoints

### Xem Monitoring Alerts

- **Method:** `GET api/admin/monitoring-alerts`
- **Auth:** Required (Admin)
- **Response:** List `MonitoringAlertDto`

### Acknowledge Alert

- **Method:** `POST api/admin/monitoring-alerts/{alertId}/acknowledge`
- **Auth:** Required (Admin)
- **Response:** `204 No Content`

### Resolve Alert

- **Method:** `POST api/admin/monitoring-alerts/{alertId}/resolve`
- **Auth:** Required (Admin)
- **Response:** `204 No Content`

### Cancel Invalid Bid (Admin)

- **Method:** `POST api/admin/auctions/{auctionId}/bids/{bidId}/cancel`
- **Auth:** Required (Admin)
- **Response:** `204 No Content`
- **Ghi chu:** Admin co the cancel mot bid cu the neu phat hien la fraudulent

### Flag User

- **Method:** `POST api/admin/users/{userId}/risk-flags`
- **Auth:** Required (Admin)

### Flag Auction

- **Method:** `POST api/admin/auctions/{auctionId}/alerts`
- **Auth:** Required (Admin)

## Configuration (RuntimeSettings)

| Setting                                  | Mo ta                                    | Mac dinh |
|------------------------------------------|------------------------------------------|----------|
| `Monitoring.InvalidBidBurstThreshold`    | So lan that bai de trigger alert         | int      |

## MonitoringAlert Status

```
Open ──[Acknowledge]──> Acknowledged ──[Resolve]──> Resolved
```

## Luu y nghiep vu

- Giam sat dua tren **userId va IP address** - phat hien ca truong hop user dung nhieu tai khoan hoac bot tu cung IP
- Window 10 phut de tranh false positive tu nguoi dung that su nhap sai
- Threshold cau hinh duoc qua `RuntimeSettings` - co the dieu chinh tuy theo muc do risk
- Neu threshold = 0, giam sat bi tat (chi ghi audit log)
- Alert khong tu dong ban user - admin can review va quyet dinh hanh dong
- He thong cung ho tro `ScanActiveAuctionsForCollusionJob` de phat hien collusion (thong dong) giua cac bidder
