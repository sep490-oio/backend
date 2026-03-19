# Runner-Up Offer

## Tong quan

Khi nguoi thang dau gia khong thanh toan (trang thai `PaymentDefaulted`), seller co the gui de nghi cho nguoi dat gia cao thu hai (runner-up). Nguoi nhan co the chap nhan hoac tu choi. Neu chap nhan, ho tro thanh winner moi.

## Actors

- **Seller (Nguoi ban):** Gui de nghi cho runner-up
- **Runner-up (Nguoi mua):** Chap nhan hoac tu choi de nghi

## Endpoint Sequence

### Step 1: Gui de nghi cho Runner-up

- **Method:** `POST api/auctions/{auctionId}/runner-up-offers`
- **Auth:** Required (Seller, owner cua Item)
- **Request:** Khong co body
- **Response:** `200 OK` - `WinnerOfferDto`
- **Ghi chu:**
  - Auction phai o trang thai `PaymentDefaulted`
  - He thong tu dong tim runner-up tiep theo chua duoc gui de nghi
  - Moi thoi diem chi co **mot** offer active
  - Thoi han de nghi cau hinh qua `RuntimeSettings.Auction.RunnerUpOfferExpirationHours`

### Step 2: Phan hoi de nghi

- **Method:** `POST api/auctions/{auctionId}/runner-up-offers/respond`
- **Auth:** Required (Runner-up duoc moi)
- **Request:**
  ```json
  {
    "accept": true
  }
  ```
- **Response:** `200 OK` - `WinnerOfferDto`
- **Ghi chu:**
  - Chi nguoi nhan de nghi moi co quyen phan hoi
  - `accept: true` -> Chap nhan, tro thanh winner moi
  - `accept: false` -> Tu choi, seller co the gui de nghi cho nguoi khac

## Business Logic

### Tim Runner-up

`auction.OfferRunnerUp(nowUtc, expirationWindow)`:

1. Kiem tra status == `PaymentDefaulted`
2. Expire tat ca pending offers da het han
3. Kiem tra khong co offer active
4. Lay ranked bids (sap xep theo amount DESC, thoi gian ASC)
5. Loai bo nhung bidder da nhan offer truoc do (tru cancelled offers)
6. Loai bo winner hien tai
7. Tim ung vien tiep theo -> tao `AuctionWinnerOffer`

### Xep hang Bids

`GetRankedBids()`:
- Lay tat ca bids co status: Outbid, Winning, Won, Cancelled
- Group theo BidderId
- Moi nhom lay bid cao nhat (amount DESC, thoi gian ASC)
- Sap xep ket qua theo amount DESC, thoi gian ASC

### Phan hoi de nghi

`auction.RespondToRunnerUpOffer(userId, accept, nowUtc)`:

**Neu accept = true:**
1. Danh dau offer la `Accepted`
2. Goi `TransferToRunnerUp(nowUtc)`:
   - Cancel winner bids cu (tru runner-up bid)
   - Cap nhat Pricing voi gia cua runner-up
   - Set WinnerId = runner-up BidderId
   - Mark runner-up bid la `Won`
   - Status -> `Sold`
   - Raise `AuctionSoldEvent`

**Neu accept = false:**
1. Danh dau offer la `Declined`
2. Seller co the gui de nghi cho nguoi khac

## Background Jobs

| Job                       | Schedule  | Mo ta                              |
|---------------------------|-----------|------------------------------------|
| `ExpireRunnerUpOffersJob` | Recurring | Tu dong expire offers het han      |

## Domain Events

| Event              | Khi nao                                    |
|--------------------|--------------------------------------------|
| `AuctionSoldEvent` | Runner-up chap nhan -> chuyen thanh Sold   |

## Luu y nghiep vu

- Seller co the gui de nghi cho nhieu runner-up lien tiep (neu nguoi truoc tu choi), nhung chi **mot offer active tai mot thoi diem**
- Gia ban la gia bid cua runner-up (khong phai gia cua winner cu)
- Gia moi co the thap hon gia cu (vi runner-up dat gia thap hon winner)
- He thong tu dong expire offers het han qua `ExpireRunnerUpOffersJob`
- Neu khong con runner-up nao, he thong tra ve loi `NoMoreRunnerUps`
- Seller cung co the chon `Relist` thay vi gui de nghi cho runner-up
