# Payment Default (Buyer Khong Thanh Toan)

## Tong quan

Khi buyer khong thanh toan trong thoi han, order bi huy va auction chuyen sang trang thai `PaymentDefaulted`. Seller co the chon offer cho runner-up (nguoi co gia cao thu hai) hoac relist auction.

## Actors

- **System** - tu dong huy order khi het han
- **Seller** - quyet dinh offer runner-up hoac relist
- **Runner-up** - nhan offer va quyet dinh chap nhan/tu choi

## Flow sau Payment Default

### Buoc 1: Order bi huy tu dong
- Xem chi tiet tai [cancel-expired.md](./cancel-expired.md)

### Buoc 2: Seller Offer Runner-Up
- **Method:** `POST /api/auctions/{auctionId}/runner-up-offers`
- **Auth:** Required (Seller)
- **Ghi chu:** Chi co the offer khi auction o trang thai PaymentDefaulted

### Buoc 3: Runner-Up Respond
- **Method:** `POST /api/auctions/{auctionId}/runner-up-offers/respond`
- **Auth:** Required (Runner-up)
- **Ghi chu:** Runner-up co the Accept hoac Decline

### Buoc 4a: Neu Accept
- Tao Order moi cho runner-up
- Order co `PendingPayment` voi payment deadline moi

### Buoc 4b: Neu Decline hoac Het han
- Seller co the relist auction
- **Method:** `POST /api/auctions/{auctionId}/relist`

## Background Job: ExpireRunnerUpOffersJob

- Tu dong expire cac runner-up offer het han
- Cho phep seller quyet dinh buoc tiep theo

## Lien ket

- Deposit cua winner bi payment default: co the bi forfeit
  - `ForfeitAuctionDepositCommand` -> tich thu deposit
  - `ReturnAuctionDepositCommand` -> hoan deposit

## Luu y nghiep vu

- Payment default la vi pham nghiem trong, tao risk flag
- Seller khong bi thiet hai vi chua nhan tien (escrow chua tao)
- Runner-up offer co thoi han rieng
- Deposit cua winner default co the bi tich thu theo chinh sach
