# Quan Ly Vai Tro & Quyen (Role & Permission)

## Tong quan

Admin co the phan quyen cho user thong qua vai tro (role) va quyen rieng le (permission). He thong ho tro RBAC (Role-Based Access Control) voi kha nang grant/deny/revoke permission o cap user.

## Actors

- **Admin** - nguoi co quyen `ManageUsers`

## Endpoint Sequence

### Xem danh sach vai tro
- **Method:** `GET /api/admin/roles`
- **Auth:** Required (Permission: `ManageUsers`)
- **Response:** `200 OK`

### Xem danh sach quyen
- **Method:** `GET /api/admin/permissions`
- **Auth:** Required (Permission: `ManageUsers`)
- **Response:** `200 OK`

### Gan vai tro cho user
- **Method:** `PUT /api/admin/users/{userId}/roles/{role}`
- **Auth:** Required (Permission: `ManageUsers`)
- **Ghi chu:** Them role cho user (additive)

### Thu hoi vai tro
- **Method:** `DELETE /api/admin/users/{userId}/roles/{role}`
- **Auth:** Required (Permission: `ManageUsers`)

### Cap quyen rieng cho user
- **Method:** `PUT /api/admin/users/{userId}/permissions/{permission}`
- **Auth:** Required (Permission: `ManageUsers`)
- **Ghi chu:** Grant permission truc tiep cho user (khong qua role)

### Tu choi quyen
- **Method:** `POST /api/admin/users/{userId}/permissions/{permission}`
- **Auth:** Required (Permission: `ManageUsers`)
- **Ghi chu:** Deny - user se khong co quyen nay du role co

### Thu hoi quyen
- **Method:** `DELETE /api/admin/users/{userId}/permissions/{permission}`
- **Auth:** Required (Permission: `ManageUsers`)
- **Ghi chu:** Revoke - xoa grant/deny, tro ve mac dinh theo role

### Toggle quyen trong role
- **Method:** `POST /api/admin/roles/{role}/permissions/{permission}`
- **Auth:** Required (Permission: `ManageUsers`)
- **Ghi chu:** Bat/tat permission trong role

## Permission Resolution

```
User Permission = Role Permissions + User Grants - User Denies
```

1. Lay tat ca permissions tu roles cua user
2. Them cac permissions duoc grant truc tiep
3. Bo cac permissions bi deny truc tiep
4. Ket qua la danh sach quyen thuc te cua user

## Luu y nghiep vu

- Deny co do uu tien cao hon Grant
- Mot user co the co nhieu role
- Permission duoc kiem tra tren moi endpoint qua `RequireAuthorization`
- Admin khong the tu thu hoi quyen `ManageUsers` cua chinh minh
