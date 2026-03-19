# Auto-Bid Pause & Resume

## Tong quan

Bidder co the tam dung (pause) va tiep tuc (resume) auto-bid cua minh bat ky luc nao khi phien dau gia dang Active. Khi pause, auto-bid se khong tu dong dat gia khi bi outbid. Khi resume, auto-bid se lap tuc tham gia lai va co the dat gia ngay neu dang bi outbid.

## Actors

- **Bidder (Nguoi mua):** Pause va resume auto-bid

## Endpoint Sequence

### Pause Auto-Bid

- **Method:** `POST api/auctions/{auctionId}/auto-bid/pause`
- **Auth:** Required
- **Request:** Khong co body
- **Response:** `204 No Content`

### Resume Auto-Bid

- **Method:** `POST api/auctions/{auctionId}/auto-bid/resume`
- **Auth:** Required
- **Request:** Khong co body
- **Response:** `204 No Content`

### Xem Auto-Bid cua toi

- **Method:** `GET api/auctions/{auctionId}/auto-bid/my`
- **Auth:** Required
- **Response:** `200 OK` - `AutoBidDto`

## Business Logic

### PauseAutoBidCommand -> AuctionGrain.PauseAutoBidAsync

```
FindAutoBid(bidderId)
    |
    v
autoBid.Pause(nowUtc)
    |
    v
[autoBid.IsEnabled = false]
    |
    v
SaveAsync()
```

- Tim auto-bid cua bidder hien tai
- Goi `autoBid.Pause(nowUtc)` de tat `IsEnabled`
- Khi pause, auto-bid se khong tham gia vao `ProcessAutoBids` (vi dieu kien `IsEnabled == true`)

### ResumeAutoBidCommand -> AuctionGrain.ResumeAutoBidAsync

```
EnsureAcceptsBids(nowUtc) -> Auction phai Active
    |
EnsureNotLockedByBuyNowReservation(nowUtc)
    |
FindAutoBid(bidderId)
    |
autoBid.Resume(nowUtc)
    |
[autoBid.IsEnabled = true]
    |
EngageAutoBidAgainstCurrentWinner(autoBid, bidderId, nowUtc)
    |
[Current winner co the bi outbid?]
    |           |
  [Co]       [Khong]
    |           |
  Dat gia     (done)
  ngay
    |
SaveAsync()
```

- Kiem tra phien dau gia van dang Active
- Khong co active buy-now reservation
- Resume auto-bid (bat `IsEnabled` lai)
- **Engage ngay:** Sau khi resume, he thong kiem tra xem auto-bid co the outbid current winner khong. Neu co -> dat gia ngay lap tuc

## Wallet Hold Behavior

- **Khi pause:** Wallet hold **KHONG bi giai phong**. So tien van duoc hold de dam bao khi resume, bidder van co du tien
- **Khi resume:** Khong can hold them (vi hold van con tu truoc)
- **De giai phong hold:** Bidder phai huy auto-bid hoan toan (khong chi pause)

## Luu y nghiep vu

- Pause chi co tac dung khi auto-bid dang o status `Active`
- Resume chi co tac dung khi phien dau gia van Active va khong co buy-now reservation
- Sau khi resume, auto-bid co the dat gia ngay lap tuc (EngageAutoBidAgainstCurrentWinner)
- Wallet hold duoc giu nguyen khi pause - day la thiet ke co chu dich de tranh tinh trang bidder khong co du tien khi resume
- Neu bidder muon lay lai tien, ho can doi phien dau gia ket thuc hoac he thong tu dong unhold khi auction resolve
