# Xu Ly Tinh Huong Khan Cap Auction (Emergency)

## Tong quan

Admin co the tao tinh huong khan cap (emergency) cho auction dang dien ra, vi du: phat hien lua dao, vi pham nghiem trong, loi he thong. Emergency co the dan den dinh chi auction.

## Actors

- **Admin** - nguoi co quyen `ManageItems`

## Endpoint Sequence

### Step 1: Tao Auction Emergency
- **Method:** `POST /api/admin/auctions/{auctionId}/emergencies`
- **Auth:** Required (Permission: `ManageItems`)
- **Ghi chu:** Tao `TriggerAuctionEmergencyCommand`

### Step 2: Giai quyet Emergency
- **Method:** `POST /api/admin/auctions/{auctionId}/emergencies/{emergencyId}/resolve`
- **Auth:** Required (Permission: `ManageItems`)
- **Ghi chu:** Tao `ResolveAuctionEmergencyCommand`

## Hanh dong co the

### Khi tao Emergency:
- Tam dung auction (pause bidding)
- Thong bao cho tat ca participant
- Ghi log audit

### Khi resolve Emergency:
- **Resume:** Tiep tuc auction binh thuong
- **Terminate:** Ket thuc auction va huy tat ca bid
- **Cancel:** Huy auction va hoan deposit cho tat ca participant

## Lien ket voi Report Escalation

- Admin co the escalate report thanh auction emergency:
  - `POST /api/admin/reports/{reportId}/escalate-emergency`
  - Tu dong tao emergency cho auction lien quan

## Luu y nghiep vu

- Emergency la co che xu ly nhanh cho tinh huong nghiem trong
- Chi admin moi co quyen tao va resolve emergency
- Auction bi emergency co the tiep tuc hoac bi huy tuy quyet dinh admin
- Tat ca hanh dong duoc ghi audit log
- Participant nhan thong bao khi co emergency
