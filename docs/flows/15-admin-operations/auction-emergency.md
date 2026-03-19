# Xu Ly Khan Cap Auction (Emergency)

## Tong quan

Admin co the tao tinh huong khan cap cho auction dang dien ra khi phat hien vi pham nghiem trong. Bao gom trigger emergency, resolve, va huy bid bat hop le.

## Actors

- **Admin** - nguoi co quyen `ManageItems`

## Endpoint Sequence

### Tao Emergency
- **Method:** `POST /api/admin/auctions/{auctionId}/emergencies`
- **Auth:** Required (Permission: `ManageItems`)
- **Ghi chu:** `TriggerAuctionEmergencyCommand`

### Resolve Emergency
- **Method:** `POST /api/admin/auctions/{auctionId}/emergencies/{emergencyId}/resolve`
- **Auth:** Required (Permission: `ManageItems`)
- **Ghi chu:** `ResolveAuctionEmergencyCommand`

### Huy Bid Bat Hop Le
- **Method:** `POST /api/admin/auctions/{auctionId}/bids/{bidId}/cancel`
- **Auth:** Required (Permission: `ManageItems`)
- **Ghi chu:** `CancelInvalidBidCommand` - huy bid khi phat hien thao tung gia

## Flow

```
Phat hien vi pham -> Trigger Emergency -> [Dieu tra]
  -> Resume (tiep tuc) | Terminate (ket thuc) | Cancel (huy)
```

## Lien ket voi Report Escalation

- Report vi pham co the duoc escalate thanh emergency
- `POST /api/admin/reports/{reportId}/escalate-emergency`

## Luu y nghiep vu

- Emergency dung auction ngay lap tuc (tam ngung bidding)
- Admin phai resolve emergency de auction tiep tuc hoac ket thuc
- Huy bid la hanh dong nghiem trong, can ghi ly do
- Tat ca hanh dong duoc ghi audit log
