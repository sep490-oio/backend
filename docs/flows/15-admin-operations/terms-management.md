# Quan Ly Dieu Khoan (Terms Management)

## Tong quan

Admin co the tao va quan ly cac tai lieu dieu khoan su dung (Terms of Service, Privacy Policy, ...). User phai chap nhan terms truoc khi su dung he thong.

## Actors

- **Admin** - tao va kich hoat terms
- **User** - chap nhan terms

## Admin Endpoints

### Xem danh sach terms
- **Method:** `GET /api/admin/terms`
- **Auth:** Required (Permission: admin)
- **Response:** `200 OK`

### Tao terms moi
- **Method:** `POST /api/admin/terms`
- **Auth:** Required (Permission: admin)
- **Ghi chu:** `CreateTermsDocumentCommand`

### Kich hoat terms
- **Method:** `POST /api/admin/terms/{id}/activate`
- **Auth:** Required (Permission: admin)
- **Ghi chu:** `ActivateTermsDocumentCommand` - chi 1 terms cung type duoc active tai mot thoi diem

## Public Endpoints

### Xem terms dang active
- **Method:** `GET /api/terms/active`
- **Auth:** Anonymous
- **Response:** `200 OK`

### Xem terms theo type
- **Method:** `GET /api/terms/{type}/active`
- **Auth:** Anonymous
- **Response:** `200 OK`

## User Endpoints

### Chap nhan terms
- **Method:** `POST /api/me/terms/{termDocumentId}/accept`
- **Auth:** Required
- **Ghi chu:** `AcceptTermsCommand`

### Xem terms da chap nhan
- **Method:** `GET /api/me/terms`
- **Auth:** Required

## Terms Status

```
Draft -> [Activate] -> Active -> [Activate version moi] -> Archived
```

## Luu y nghiep vu

- Khi activate terms moi, version cu tu dong chuyen sang Archived
- User phai chap nhan terms moi truoc khi tiep tuc su dung (middleware check)
- Lich su chap nhan terms duoc luu de phap ly
- Terms co nhieu type: tos (Terms of Service), privacy (Privacy Policy), ...
- Chi 1 terms cung type duoc active tai mot thoi diem
