# 07 - Bidding

## Tong quan

Module nay quan ly tat ca cac hinh thuc dat gia trong phien dau gia OIO: manual bid, auto-bid, sealed bid, va cac tinh nang ho tro nhu deposit qualification, watch, va invalid bid detection.

Moi phien dau gia yeu cau nguoi mua phai dang ky tham gia (qualification) truoc khi dat gia. He thong su dung Orleans Grain (`AuctionGrain`) de dam bao single-threaded access va tranh race conditions.

## Cac loai dat gia

### 1. Manual Bid (Dat gia thu cong)
- Bidder tu nhap gia va dat
- Ho tro qua REST API va SignalR hub
- Trigger auto-bid cascade tu bidders khac

### 2. Auto-Bid (Dat gia tu dong)
- Bidder cau hinh max amount va (tuy chon) increment
- He thong tu dong dat gia khi bi outbid
- Auto-bid battle: hai auto-bidder canh tranh tu dong
- Cascade cap: toi da 200 operations de tranh infinite loop

### 3. Sealed Bid (Dat gia kin)
- Chi danh cho auction type `sealed`
- Bidder gui encrypted amount
- Admin reveal sau khi het thoi gian
- Bid cao nhat thang (ties: nguoi dat truoc thang)

## Actors

- **Bidder (Nguoi mua):** Dat gia, cau hinh auto-bid, theo doi auction
- **Seller (Nguoi ban):** Khong duoc dat gia tren auction cua minh
- **Admin:** Reveal sealed bids, cancel invalid bids, xu ly monitoring alerts
- **System:** Xu ly auto-bid cascade, giam sat invalid bids

## Dieu kien tham gia (Participant Eligibility)

Truoc khi dat gia, bidder phai:
1. Dang ky tham gia trong `QualificationWindow`
2. Dat coc qua VNPay -> wallet credit -> hold
3. Duoc he thong xac nhan la `AuctionParticipant` voi `IsQualified = true`

Chi co participants du dieu kien moi duoc phep dat gia khi phien dau gia Active.

## Subflow Files

| File                                                        | Mo ta                                           |
|-------------------------------------------------------------|--------------------------------------------------|
| [deposit-qualification.md](./deposit-qualification.md)      | Dat coc va dang ky tham gia                     |
| [manual-bid.md](./manual-bid.md)                            | Dat gia thu cong (REST + SignalR)                |
| [auto-bid-configure.md](./auto-bid-configure.md)            | Cau hinh auto-bid                               |
| [auto-bid-battle.md](./auto-bid-battle.md)                  | Auto-bid cascade va battle logic                |
| [auto-bid-pause-resume.md](./auto-bid-pause-resume.md)      | Tam dung va tiep tuc auto-bid                   |
| [sealed-bid.md](./sealed-bid.md)                            | Dat gia kin va reveal                           |
| [watch-auction.md](./watch-auction.md)                      | Theo doi phien dau gia                          |
| [invalid-bid-detection.md](./invalid-bid-detection.md)      | Giam sat va phat hien bid bat thuong            |

## SignalR Hub

- **Hub URL:** `/hubs/auction`
- **Auth:** Required (Bearer token)
- **Connection:** Tham gia group `auction:{auctionId}` khi vao phong dau gia

### Client -> Server Methods

| Method             | Mo ta                          | Permission              |
|--------------------|--------------------------------|--------------------------|
| `JoinAuction`      | Tham gia phong dau gia         | (none)                   |
| `LeaveAuction`     | Roi phong dau gia              | (none)                   |
| `PlaceBid`         | Dat gia                        | `Auctions.Bid`           |
| `BuyNow`           | Mua ngay                       | `Auctions.BuyNow`        |
| `ConfigureAutoBid` | Cau hinh auto-bid              | `Auctions.AutoBid`       |
| `WatchAuction`     | Theo doi phien dau gia         | `Auctions.Watch`         |

### Server -> Client Events

| Event                       | Mo ta                                |
|-----------------------------|--------------------------------------|
| `BidPlaced`                 | Co bid moi                           |
| `Outbid`                    | Bidder bi vuot gia                   |
| `BuyNowReserved`            | Co buy-now reservation moi           |
| `BuyNowReservationReleased` | Buy-now reservation het han/that bai |
| `BuyNowExecuted`            | Mua ngay thanh cong                  |
| `AuctionStarted`            | Phien dau gia bat dau                |
| `AuctionEnded`              | Phien dau gia ket thuc               |
| `AuctionExtended`           | Gia han thoi gian                    |
| `AuctionCancelled`          | Phien dau gia bi huy                 |
| `PriceUpdated`              | Gia hien tai thay doi                |
| `Error`                     | Loi tu server                        |

## Technology Stack

- **Orleans Grain (`AuctionGrain`):** Dam bao single-threaded access per auction
- **SignalR (`AuctionHub`):** Real-time bidding
- **REST API (fallback):** Cho cac client khong ho tro SignalR
- **MediatR:** Command handlers
- **Wallet (Hold/Unhold):** Quan ly tien coc va auto-bid reservation
