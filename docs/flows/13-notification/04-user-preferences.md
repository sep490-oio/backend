# 04 -- User Notification Preferences

## UserNotificationPreference Entity

| Field | Type | Notes |
|---|---|---|
| `Id` | `UserNotificationPreferenceId` (Guid) | PK |
| `UserId` | `UserId` (Guid) | FK to User, one-to-one |
| `IsEnabled` | `bool` | Master toggle; false = no deliveries created |
| `TypePreferences` | `string` | JSONB, initialized to `"{}"` |
| `Channels` | `string` | JSONB, initialized to `"{}"` |
| `QuietHours` | `string?` | JSONB, optional |
| `RateLimits` | `string?` | JSONB, optional |
| `CreatedAt` | `DateTime` | UTC |
| `ModifiedAt` | `DateTime?` | UTC, set on Update |

### Factory Method

```csharp
UserNotificationPreference.Create(UserId userId, DateTime nowUtc)
```

Creates with `IsEnabled = true`, `TypePreferences = "{}"`, `Channels = "{}"`.

### Update Method

```csharp
preference.Update(bool isEnabled, string channels, string? quietHours, DateTime nowUtc)
```

Sets `IsEnabled`, `Channels`, `QuietHours`, and `ModifiedAt`.

## GET Notification Preferences

**Endpoint:** `GET /api/me/notification-preferences`
**Auth:** Requires `Catalogs.Me.ReadNotificationPreferences` permission
**Handler:** `GetNotificationPreferenceQueryHandler`

**Logic:**
1. Load `UserNotificationPreference` where `UserId == currentUser.UserId`
2. If no record exists, return a default DTO:
   ```
   UserNotificationPreferenceDto(
       Id: Guid.Empty,
       IsEnabled: true,
       Channels: "{}",
       QuietHours: null,
       RateLimits: null,
       CreatedAt: DateTime.MinValue,
       ModifiedAt: null
   )
   ```
3. If record exists, return `preference.ToDto()`

**Response:** `UserNotificationPreferenceDto`

| Field | Type |
|---|---|
| `Id` | `Guid` |
| `IsEnabled` | `bool` |
| `Channels` | `string` (JSON) |
| `QuietHours` | `string?` (JSON) |
| `RateLimits` | `string?` (JSON) |
| `CreatedAt` | `DateTime` |
| `ModifiedAt` | `DateTime?` |

## PUT Update Notification Preferences

**Endpoint:** `PUT /api/me/notification-preferences`
**Auth:** Requires `Catalogs.Me.ManageNotificationPreferences` permission
**Handler:** `UpdateNotificationPreferenceCommandHandler`

**Request body:**

```json
{
  "isEnabled": true,
  "channels": "[\"Email\",\"SignalR\"]",
  "quietHours": null
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `IsEnabled` | `bool` | Yes | Master toggle |
| `Channels` | `string` | Yes | JSON string, e.g. `["Email"]`, `["SignalR"]`, `["Email","SignalR"]`, or `[]` |
| `QuietHours` | `string?` | No | JSON object for quiet hours config |

**Logic:**
1. Load existing `UserNotificationPreference` for current user
2. If not found, create a new one via `UserNotificationPreference.Create(userId, nowUtc)` and insert
3. Call `preference.Update(isEnabled, channels, quietHours, nowUtc)`
4. `SaveChangesAsync`
5. Return `preference.ToDto()`

**Response:** `UserNotificationPreferenceDto` (same shape as GET)

## Default Behavior (No Preference Record)

When `NotificationRoutingService.Route()` is called and no `UserNotificationPreference` exists for the user:

- Both **Email** and **SignalR** channels are enabled
- Deliveries are created for both channels

This means users receive notifications via both channels by default without needing to configure anything.

## Channel Selection Examples

| Channels JSON | Resulting deliveries |
|---|---|
| (no preference record) | Email (maxAttempts=3) + SignalR (maxAttempts=1) |
| `"[\"Email\",\"SignalR\"]"` | Email + SignalR |
| `"[\"Email\"]"` | Email only |
| `"[\"SignalR\"]"` | SignalR only |
| `"[]"` | None (but IsEnabled must be true) |
| `"{}"` (default on create) | Fallback: Email + SignalR (empty string parsed, falls through to default) |
| IsEnabled = false | None (regardless of channels) |

## Routing Service Interaction

The `NotificationRoutingService` reads the preference in this order:

1. **IsEnabled check:** If preference exists but `IsEnabled = false`, return empty (no deliveries)
2. **Parse Channels:** Try `JsonSerializer.Deserialize<string[]>(preference.Channels)`
3. **Fallback:** On null result, empty Channels string, or JSON parse exception -> default to Email + SignalR
4. **No preference:** Default to Email + SignalR
