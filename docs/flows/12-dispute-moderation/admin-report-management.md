# Admin Quan Ly Bao Cao (Report Management)

## Tong quan

Admin co the xem, phan cong, giai quyet, va nang cap cac bao cao vi pham. Bao gom luong assign reviewer, resolve (dismiss/action), va escalate emergency.

## Actors

- **Admin** - nguoi co quyen `ManageItems`

## Endpoint Sequence

### Step 1: Xem danh sach bao cao
- **Method:** `GET /api/admin/reports`
- **Auth:** Required (Permission: `ManageItems`)
- **Response:** `200 OK` -> Danh sach phan trang

### Step 2: Phan cong nguoi xu ly
- **Method:** `POST /api/admin/reports/{reportId}/assign`
- **Auth:** Required (Permission: `ManageItems`)
- **Ghi chu:** Gan admin cu the de xu ly report

### Step 3a: Giai quyet bao cao
- **Method:** `POST /api/admin/reports/{reportId}/resolve`
- **Auth:** Required (Permission: `ManageItems`)
- **Request:**
  ```json
  {
    "dismissed": true,
    "resolutionNotes": "Bao cao khong co co so"
  }
  ```
- **Response:** `200 OK` -> `ReportDto`
- **Ghi chu:**
  - `dismissed = true`: Bao cao bi bo qua (khong co vi pham)
  - `dismissed = false`: Bao cao duoc xu ly (da thuc hien hanh dong)

### Step 3b: Nang cap khan cap
- **Method:** `POST /api/admin/reports/{reportId}/escalate-emergency`
- **Auth:** Required (Permission: `ManageItems`)
- **Ghi chu:** Chuyen bao cao thanh tinh huong khan cap (vi du: lua dao dang dien ra)

## Report Flow

```
Pending -> [Assign] -> Assigned -> [Resolve] -> Resolved (Dismissed | Actioned)
                                -> [Escalate] -> Emergency
```

## Luu y nghiep vu

- Admin co the resolve voi `dismissed = true` neu bao cao khong hop le
- Resolve voi `dismissed = false` thuong kem theo hanh dong (ban user, go auction, ...)
- Escalate tao auction emergency neu lien quan den auction dang dien ra
- Resolution notes duoc luu de audit
