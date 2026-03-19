# Tuy Chon Thong Bao (Notification Preferences)

## Tong quan

Nguoi dung co the cau hinh cach nhan thong bao: bat/tat thong bao, chon kenh nhan, dat gio im lang (quiet hours), va tuy chinh theo tung loai thong bao. Cai dat duoc luu duoi dang JSON trong database.

## Actors

- **Nguoi dung da dang nhap** - xem va cap nhat tuy chon thong bao cua minh

## Endpoint Sequence

### Step 1: Xem tuy chon thong bao hien tai

- **Method:** `GET /api/me/notification-preferences`
- **Auth:** Required (Bearer JWT)
- **Response:** `200 OK`
  ```json
  {
    "id": "guid",
    "isEnabled": true,
    "channels": "{}",
    "quietHours": null,
    "rateLimits": null,
    "createdAt": "datetime",
    "modifiedAt": "datetime?"
  }
  ```
- **Ghi chu:**
  - Neu chua co preference, tra ve gia tri mac dinh: `isEnabled = true`, `channels = "{}"`, cac truong khac = `null`
  - `id` se la `Guid.Empty` khi chua tao record

### Step 2: Cap nhat tuy chon thong bao

- **Method:** `PUT /api/me/notification-preferences`
- **Auth:** Required (Bearer JWT)
- **Request:**
  ```json
  {
    "isEnabled": true,
    "channels": "{\"email\": true, \"signalR\": true, \"push\": false}",
    "quietHours": "{\"start\": \"22:00\", \"end\": \"07:00\", \"timezone\": \"Asia/Ho_Chi_Minh\"}"
  }
  ```
- **Response:** `200 OK`
  ```json
  {
    "id": "guid",
    "isEnabled": true,
    "channels": "{...}",
    "quietHours": "{...}",
    "rateLimits": null,
    "createdAt": "datetime",
    "modifiedAt": "datetime"
  }
  ```
- **Ghi chu:**
  - Neu chua co record: tu dong tao moi roi cap nhat
  - Neu da co: cap nhat record hien tai

## Domain Model

### Entity: `UserNotificationPreference`

| Truong | Kieu | Mo ta |
|---|---|---|
| `Id` | `UserNotificationPreferenceId` | Primary key |
| `UserId` | `UserId` | FK toi User |
| `IsEnabled` | `bool` | Bat/tat toan bo thong bao |
| `TypePreferences` | `string` (jsonb) | Tuy chinh theo tung loai thong bao |
| `Channels` | `string` (jsonb) | Cau hinh kenh nhan thong bao |
| `QuietHours` | `string?` (jsonb) | Gio im lang |
| `RateLimits` | `string?` (jsonb) | Gioi han tan suat thong bao |
| `CreatedAt` | `DateTime` | Thoi diem tao |
| `ModifiedAt` | `DateTime?` | Thoi diem cap nhat |

### Kenh thong bao (Channels)

| Kenh | Mo ta |
|---|---|
| `email` | Thong bao qua email |
| `signalR` | Thong bao realtime qua SignalR (trong ung dung) |
| `push` | Thong bao day (push notification) |

Vi du `channels`:
```json
{
  "email": true,
  "signalR": true,
  "push": false
}
```

### Quiet Hours

Cho phep user tat thong bao trong khoang thoi gian nhat dinh. Gia tri `null` nghia la khong ap dung quiet hours.

Vi du `quietHours`:
```json
{
  "start": "22:00",
  "end": "07:00",
  "timezone": "Asia/Ho_Chi_Minh"
}
```

### Type Preferences

Truong `TypePreferences` (jsonb) cho phep tuy chinh theo tung loai thong bao cu the (vi du: tat thong bao dau gia, chi nhan email cho don hang, ...). Mac dinh la `"{}"` (khong co override, dung cai dat chung).

## Luu y nghiep vu

- Moi user chi co **1 record** notification preference (1-1 voi User)
- Khi `isEnabled = false`: toan bo thong bao bi tat bat ke cau hinh channels
- `channels` va `quietHours` duoc luu duoi dang JSON string, frontend parse/serialize
- `rateLimits` hien tai chi duoc doc, chua co endpoint de cap nhat (he thong tu quan ly)
- Mac dinh khi chua tao preference: `isEnabled = true`, `channels = "{}"`
