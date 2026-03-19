# Watch Auction (Theo doi phien dau gia)

## Tong quan

Nguoi mua co the theo doi phien dau gia de nhan thong bao khi co bid moi hoac khi phien dau gia ket thuc. Ho tro qua SignalR hub va REST API.

## Actors

- **User (Nguoi dung):** Theo doi hoac bo theo doi phien dau gia

## Endpoint Sequence

### Watch (Theo doi)

#### Cach 1: SignalR Hub

- **Hub:** `/hubs/auction`
- **Method:** `WatchAuction(auctionId, notifyOnBid?, notifyOnEnd?)`
- **Auth:** Required (Permission: `Auctions.Watch`)
- **Response:** Error notification neu that bai

#### Cach 2: REST API

- **Method:** `POST api/auctions/{auctionId}/watch`
- **Auth:** Required
- **Request:**
  ```json
  {
    "notifyOnBid": true,
    "notifyOnEnd": true
  }
  ```
- **Response:** `204 No Content`

### Unwatch (Bo theo doi)

- **Method:** `DELETE api/auctions/{auctionId}/watch`
- **Auth:** Required
- **Request:** Khong co body
- **Response:** `204 No Content`

### Xem Watchlist cua toi

- **Method:** `GET api/me/auctions/watch-list`
- **Auth:** Required
- **Response:** `200 OK` - List auctions dang theo doi

## Business Logic

### Watch

`auction.AddWatcher(userId, nowUtc, notifyOnBid, notifyOnEnd)`:

1. Kiem tra chua theo doi truoc do (error: `AlreadyWatching`)
2. Kiem tra khong phai seller cua auction (error: `CannotWatchOwnAuction`)
3. Tao `AuctionWatcher` moi
4. Tang `WatchCount`
5. Raise `AuctionWatcherAddedEvent`

### Unwatch

`auction.RemoveWatcher(userId, nowUtc)`:

1. Tim watcher theo userId
2. Neu khong tim thay -> return (khong loi)
3. Xoa watcher
4. Giam `WatchCount` (min = 0)

### Update Preferences

`auction.UpdateWatcherPreferences(userId, nowUtc, notifyOnBid?, notifyOnEnd?)`:

1. Tim watcher theo userId (error: `NotFound`)
2. Kiem tra khong phai seller (error: `CannotWatchOwnAuction`)
3. Cap nhat notification settings

## Notification Settings

| Setting        | Mo ta                                   | Mac dinh |
|----------------|-----------------------------------------|----------|
| `notifyOnBid`  | Thong bao khi co bid moi               | `true`   |
| `notifyOnEnd`  | Thong bao khi phien dau gia ket thuc   | `true`   |

## Domain Events

| Event                      | Payload                          |
|----------------------------|----------------------------------|
| `AuctionWatcherAddedEvent` | AuctionId, UserId, OccurredAt    |

## Luu y nghiep vu

- Seller khong the theo doi phien dau gia cua minh
- Mot user chi co the theo doi mot phien dau gia mot lan (UNIQUE)
- Unwatch la idempotent - neu chua theo doi thi khong loi
- WatchCount duoc dung de hien thi do pho bien cua phien dau gia
- Watchers nhan thong bao qua notification system (khong phai SignalR truc tiep) dua tren preferences cua ho
