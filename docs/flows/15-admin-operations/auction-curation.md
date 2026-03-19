# Quan Ly Dau Gia - Curation

## Tong quan

Admin co the cau hinh curation cho auction, bao gom: featured, priority, category placement, va cac thuoc tinh hien thi dac biet.

## Actors

- **Admin** - nguoi co quyen `ManageItems`

## Endpoint

### Set Auction Curation
- **Method:** `POST /api/admin/auctions/{auctionId}/curation`
- **Auth:** Required (Permission: `ManageItems`)
- **Ghi chu:** Cau hinh cac thuoc tinh curation cho auction
  - Featured: hien thi trang chu
  - Priority: thu tu uu tien
  - Category placement: vi tri trong danh muc

## Luu y nghiep vu

- Curation khong anh huong logic dau gia, chi anh huong hien thi
- Featured auction hien thi noi bat tren trang chu
- Priority cao hon hien thi truoc trong danh sach
- Chi co the curation auction o trang thai Active hoac Published
