# Mo Sealed Bid (Sealed Bid Reveal)

## Tong quan

Trong dau gia kin (sealed bid auction), cac bid duoc an cho den khi auction ket thuc. Admin co quyen mo (reveal) sealed bid de xem noi dung truoc khi cong bo ket qua.

## Actors

- **Admin** - nguoi co quyen `ManageItems`

## Endpoint

### Reveal Sealed Bid
- **Method:** `POST /api/admin/auctions/{auctionId}/sealed-bids/{sealedBidId}/reveal`
- **Auth:** Required (Permission: `ManageItems`)
- **Ghi chu:** `AdminRevealSealedBidCommand`

## Business Logic

- Tim sealed bid theo `sealedBidId` trong auction `auctionId`
- Giai ma noi dung bid (neu duoc ma hoa)
- Tra ve thong tin bid cho admin

## Luu y nghiep vu

- Sealed bid la hinh thuc dau gia kin - nguoi dat gia khong biet gia cua nguoi khac
- Reveal chi co the thuc hien boi admin
- Sau khi auction ket thuc, tat ca sealed bid duoc reveal de xac dinh nguoi thang
- Admin co the reveal truoc de kiem tra tinh hop le (trong truong hop nghi ngo gian lan)
- Moi hanh dong reveal duoc ghi audit log
