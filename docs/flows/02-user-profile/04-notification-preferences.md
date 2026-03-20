# 06 - Notification Preferences (Tuy chinh thong bao)

## Endpoints

### 1. Xem Notification Preferences

| Thuoc tinh | Gia tri |
|-----------|---------|
| Method | `GET` |
| Route | `/api/me/notification-preferences` |
| Auth | `users:me:notification-preferences:read` (`Me.ReadNotificationPreferences`) |
| Success Response | `200 OK` |
| Response Body | `UserNotificationPreferenceDto` |
| Query | `GetNotificationPreferenceQuery` |

#### Business Logic

1. Query `UserNotificationPreference` theo `userId` (AsNoTracking)
2. **Neu chua co** (preference la null) → tra ve gia tri mac dinh:
   ```json
   {
     "id": "00000000-0000-0000-0000-000000000000",
     "isEnabled": true,
     "channels": "{}",
     "quietHours": null,
     "rateLimits": null,
     "createdAt": "0001-01-01T00:00:00",
     "modifiedAt": null
   }
   ```
3. **Neu da co** → map sang `UserNotificationPreferenceDto` va tra ve

---

### 2. Cap nhat Notification Preferences

| Thuoc tinh | Gia tri |
|-----------|---------|
| Method | `PUT` |
| Route | `/api/me/notification-preferences` |
| Auth | `users:me:notification-preferences:manage` (`Me.ManageNotificationPreferences`) |
| Success Response | `200 OK` |
| Response Body | `UserNotificationPreferenceDto` |
| Command | `UpdateNotificationPreferenceCommand` |

#### Request Body

```json
{
  "isEnabled": true,
  "channels": "{}",
  "quietHours": "{}"
}
```

| Field | Type | Required | Mo ta |
|-------|------|----------|-------|
| `IsEnabled` | `bool` | Yes | Bat/tat toan bo thong bao |
| `Channels` | `string` | Yes | JSON string cau hinh kenh thong bao |
| `QuietHours` | `string?` | No | JSON string cau hinh gio im lang |

#### Business Logic

1. **Tim preference**: Query `UserNotificationPreference` theo `userId`
2. **Neu chua co** → tao moi: `UserNotificationPreference.Create(userId, nowUtc)` voi gia tri mac dinh (`IsEnabled = true`, `TypePreferences = "{}"`, `Channels = "{}"`), sau do insert vao DB
3. **Cap nhat**: Goi `preference.Update(isEnabled, channels, quietHours, nowUtc)`:
   - Set `IsEnabled`, `Channels`, `QuietHours`, `ModifiedAt`
4. **Luu va tra ve**: SaveChanges, tra ve `UserNotificationPreferenceDto`

---

## Delivery Channels

He thong ho tro 3 kenh giao thong bao (`NotificationChannel`):

| Gia tri | Mo ta |
|---------|-------|
| `In App` | Thong bao trong ung dung (luu trong DB, hien thi tren UI) |
| `Email` | Gui qua email |
| `SignalR` | Thong bao real-time qua SignalR WebSocket |

---

## Response Schema (UserNotificationPreferenceDto)

```json
{
  "id": "guid",
  "isEnabled": true,
  "channels": "{}",
  "quietHours": null,
  "rateLimits": null,
  "createdAt": "datetime",
  "modifiedAt": "datetime | null"
}
```

---

## UserNotificationPreference Entity

| Property | Type | Mo ta |
|----------|------|-------|
| `Id` | `UserNotificationPreferenceId` | ID rieng (Guid), khac voi UserId |
| `UserId` | `UserId` | Lien ket den User |
| `IsEnabled` | `bool` | Bat/tat toan bo thong bao, mac dinh `true` |
| `TypePreferences` | `string` | JSON — cau hinh theo tung loai thong bao, mac dinh `"{}"` |
| `Channels` | `string` | JSON — cau hinh kenh nhan thong bao, mac dinh `"{}"` |
| `QuietHours` | `string?` | JSON — cau hinh gio im lang |
| `RateLimits` | `string?` | JSON — cau hinh gioi han tan suat thong bao |
| `CreatedAt` | `DateTime` | Thoi diem tao |
| `ModifiedAt` | `DateTime?` | Thoi diem cap nhat gan nhat |

> **Luu y**: `TypePreferences` va `RateLimits` khong duoc expose trong request body cua endpoint cap nhat hien tai. Chung duoc quan ly noi bo hoac boi cac he thong khac.

---

## Error Codes

| HTTP Status | Error Code | Mo ta | Khi nao |
|------------|------------|-------|---------|
| 401 | `Auth.Authentication.Required` | Chua dang nhap | Token khong hop le hoac het han |
| 403 | `Auth.Insufficient.Permissions` | Khong du quyen | User khong co permission tuong ung |

---

## Source Files

| Layer | File |
|-------|------|
| Command | `src/core/OIO.Application/Context/UserContext/Commands/UpdateNotificationPreference/UpdateNotificationPreferenceCommand.cs` |
| Query | `src/core/OIO.Application/Context/UserContext/Queries/GetNotificationPreference/GetNotificationPreferenceQuery.cs` |
| Domain | `src/core/OIO.Domain/Context/UserContext/Aggregates/Users/UserNotificationPreference.cs` |
| Channel Enum | `src/core/OIO.Domain/Context/NotificationContext/Enums/NotificationChannel.cs` |
| DTO | `src/core/OIO.Application/Context/UserContext/DTOs/UserNotificationPreferenceDto.cs` |
| Mappings | `src/core/OIO.Application/Context/UserContext/Mappings/UserNotificationPreferenceMappings.cs` |
| Endpoint (GET) | `src/presentation/OIO.Api/Endpoints/UserContext/Me/GetNotificationPreferencesEndpoint.cs` |
| Endpoint (PUT) | `src/presentation/OIO.Api/Endpoints/UserContext/Me/UpdateNotificationPreferencesEndpoint.cs` |
| Permissions | `src/core/OIO.Domain/AppDefinitions/AppPermissions.cs` |
