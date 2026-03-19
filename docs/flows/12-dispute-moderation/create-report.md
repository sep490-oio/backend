# User Bao Cao Vi Pham (Create Report)

## Tong quan

User co the bao cao vi pham doi voi cac entity trong he thong (auction, item, user, ...). Report duoc gui den admin de review va xu ly.

## Actors

- **User** - nguoi bao cao
- **Admin** - nguoi xu ly bao cao

## Endpoint Sequence

### Step 1: Tao bao cao
- **Method:** `POST /api/reports`
- **Auth:** Required
- **Request:**
  ```json
  {
    "entityType": "Auction | Item | User",
    "entityId": "guid",
    "reasonCode": "fraud | inappropriate | counterfeit | harassment",
    "description": "Mo ta chi tiet vi pham",
    "attachments": "url-to-evidence"
  }
  ```
- **Response:** `200 OK` -> `ReportDto`
- **Error cases:**
  - `409 Conflict` - User da bao cao entity nay roi

### Step 2: Xem bao cao cua toi
- **Method:** `GET /api/me/reports`
- **Auth:** Required
- **Response:** `200 OK` -> `ReportDto[]`

## Business Logic

- Moi user chi co the bao cao 1 entity 1 lan (idempotency)
- Report duoc tao voi trang thai `Pending`
- Admin se nhan thong bao ve report moi

## Report Status

```
Pending -> Assigned -> Resolved (Dismissed | Actioned)
```

## Luu y nghiep vu

- `entityType` xac dinh loai doi tuong bi bao cao
- `reasonCode` giup admin phan loai va uu tien xu ly
- User co the dinh kem bang chung (attachments URL)
- Report trung lap (cung user + cung entity) bi tu choi voi 409 Conflict
