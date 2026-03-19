# Deposit & Qualification

## Tong quan

Truoc khi tham gia dat gia, nguoi mua phai dat coc thong qua VNPay. Tien coc duoc credit vao wallet cua nguoi mua, sau do he thong hold so tien tuong ung va dang ky nguoi mua la `AuctionParticipant`. Chi nhung participant da qualified moi duoc phep dat gia khi phien dau gia Active.

## Actors

- **Bidder (Nguoi mua):** Dat coc de tham gia
- **System:** Xu ly payment callback, dang ky participant

## Endpoint Sequence

### Step 1: Tao VNPay Payment URL cho Deposit

- **Method:** `POST api/payments/vnpay/create-url`
- **Auth:** Required
- **Request:**
  ```json
  {
    "amount": 1000000,
    "currency": "VND",
    "purpose": "auction_deposit",
    "ipAddress": "127.0.0.1",
    "description": "AuctionDeposit - Auction #guid",
    "auctionId": "guid"
  }
  ```
- **Response:** `200 OK`
  ```json
  {
    "paymentUrl": "https://sandbox.vnpayment.vn/paymentv2/...",
    "transactionId": "guid"
  }
  ```

### Step 2: Nguoi mua thanh toan tren VNPay

- Redirect nguoi mua den `paymentUrl`
- Nguoi mua hoan tat thanh toan tren giao dien VNPay

### Step 3: VNPay Callback (IPN)

- **Method:** `GET api/payments/vnpay/ipn`
- **Auth:** Anonymous (VNPay server-to-server)
- **Xu ly:**
  1. Xac thuc signature tu VNPay
  2. Tim transaction trong DB
  3. Cap nhat trang thai transaction
  4. Neu thanh toan thanh cong:
     - Credit wallet cua bidder
     - Hold so tien deposit
     - Goi `auction.RegisterParticipantFromDeposit(userId, nowUtc, "bidder")`

### Step 4: Dang ky Participant

`auction.RegisterParticipantFromDeposit(userId, nowUtc, roleInAuction)`:

1. **Kiem tra khong phai seller:** userId != Item.SellerId
2. **Kiem tra timing:** Auction phai co `AuctionInfo`
3. **Kiem tra qualification window:** `Info.HasQualification && Info.IsQualificationOpen(nowUtc)`
4. **Kiem tra trung lap:** Neu da co participant (khong phai Withdrawn) -> tra ve participant hien tai
5. **Tao participant moi:** `AuctionParticipant.Create(auctionId, userId, roleInAuction, nowUtc)`
6. Cap nhat `ModifiedAt`

## Dieu kien Eligible de Dat gia

Khi phien dau gia Active, `EnsureBidderEligible(bidderId, nowUtc)` kiem tra:
- Participant phai ton tai trong `_participants`
- `IsBidEligibleParticipant(participant, nowUtc)` phai tra ve `true`:
  - JoinStatus khong phai `Withdrawn`
  - `IsQualified == true`
  - Co deposit dang duoc hold

## Qualification Window

- Duoc cau hinh khi `SetTiming` hoac `UpdateAuction`
- Bao gom `qualificationStartAt` va `qualificationEndAt`
- Nguoi mua chi co the dat coc va dang ky trong khoang thoi gian nay
- Qualification window phai ket thuc truoc hoac bang `StartTime` cua phien dau gia

## Luu y nghiep vu

- Tien coc duoc hold trong wallet, khong phai tru thang
- Neu phien dau gia ket thuc va nguoi mua khong thang, deposit duoc unhold (tra lai)
- Neu nguoi mua thang, deposit co the duoc ap dung vao thanh toan
- Seller khong duoc tham gia dat gia tren phien dau gia cua minh
- Mot nguoi mua chi co mot participant record cho moi phien dau gia (UNIQUE constraint)
- Participant co role: "bidder" (dat gia thuong), "buy_now" (mua ngay)
