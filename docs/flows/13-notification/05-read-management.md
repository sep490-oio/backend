# 05 -- Read Management

## GET My Notifications

**Endpoint:** `GET /api/notifications?page={page}&pageSize={pageSize}`
**Auth:** `RequireAuthorization()` (any authenticated user)
**Handler:** `GetMyNotificationsQueryHandler`
**Tag:** `Notifications`

**Query parameters:** `PagedParameters` (page, pageSize)

**Logic:**
1. Query `Notification` where `UserId == currentUser.UserId`
2. Order by `CreatedAt` descending (newest first)
3. Count total matching notifications
4. Project to `NotificationDto` and apply pagination via `ToPagedListAsync`

**Response:** `PagedList<NotificationDto>`

## GET Unread Notification Count

**Endpoint:** `GET /api/notifications/unread-count`
**Auth:** `RequireAuthorization()` (any authenticated user)
**Handler:** `GetUnreadNotificationCountQueryHandler`
**Tag:** `Notifications`

**Logic:**
1. Count `Notification` where `UserId == currentUser.UserId AND Status == NotificationStatus.Unread`
2. Return count

**Response:**

```json
{
  "count": 5
}
```

**Response type:** `GetUnreadNotificationCountResponse(int Count)`

## PATCH Mark Notification as Read

**Endpoint:** `PATCH /api/notifications/{notificationId}/read`
**Auth:** `RequireAuthorization()` (any authenticated user)
**Handler:** `MarkNotificationAsReadCommandHandler`
**Tag:** `Notifications`

**Path parameter:** `notificationId` (Guid)

**Logic:**
1. Load `Notification` by `NotificationId`
2. Validate ownership: `notification.UserId == currentUser.UserId`
   - If not found or not owned: return `Error.NotFound("Notification.NotFound", "Notification {id} not found")`
3. Call `notification.MarkAsRead(clock.UtcNow)`
   - Sets `Status = NotificationStatus.Read`
   - Sets `ReadAt = readAt`
   - Sets `ModifiedAt = readAt`
   - Only applies if current status is not already `Read`
4. `SaveChangesAsync`
5. Log: "Marked notification {NotificationId} as read by user {UserId}"

**Response:**

```json
{
  "notificationId": "guid-value"
}
```

**Response type:** `MarkNotificationAsReadResponse(Guid NotificationId)`

**Error responses:**
- `404 Not Found` -- notification does not exist or belongs to another user

## PATCH Mark All Notifications as Read

**Endpoint:** `PATCH /api/notifications/read-all`
**Auth:** `RequireAuthorization()` (any authenticated user)
**Handler:** `MarkAllNotificationsAsReadCommandHandler`
**Tag:** `Notifications`

**Logic:**
1. Query all `Notification` where `UserId == currentUser.UserId AND Status == NotificationStatus.Unread`
2. Load all matching notifications into memory via `ToListAsync`
3. If count is 0, return `MarkAllNotificationsAsReadResponse(0)` immediately
4. For each notification, call `notification.MarkAsRead(clock.UtcNow)`
5. `SaveChangesAsync`
6. Log: "Marked {Count} notifications as read for user {UserId}"

**Response:**

```json
{
  "updatedCount": 12
}
```

**Response type:** `MarkAllNotificationsAsReadResponse(int UpdatedCount)`

## NotificationDto

| # | Field | Type | Source |
|---|---|---|---|
| 1 | `Id` | `Guid` | `notification.Id.Value` |
| 2 | `NotificationType` | `string` | `notification.NotificationType` |
| 3 | `EventType` | `string` | `notification.EventType` |
| 4 | `Title` | `string` | `notification.Title` |
| 5 | `Message` | `string` | `notification.Message` |
| 6 | `Priority` | `string` | `notification.Priority.ToString()` |
| 7 | `Status` | `string` | `notification.Status.ToString()` |
| 8 | `EntityType` | `string?` | `notification.EntityType` |
| 9 | `EntityId` | `Guid?` | `notification.EntityId` |
| 10 | `Metadata` | `string?` | `notification.Metadata` (raw JSONB) |
| 11 | `RelatedEntities` | `string?` | `notification.RelatedEntities` (raw JSONB array) |
| 12 | `Actions` | `string?` | `notification.Actions` (raw JSONB array) |
| 13 | `CreatedAt` | `DateTime` | `notification.CreatedAt` |
| 14 | `ReadAt` | `DateTime?` | `notification.ReadAt` |
| 15 | `ExpiresAt` | `DateTime?` | `notification.ExpiresAt` |

## Notification.MarkAsRead Behavior

```csharp
public void MarkAsRead(DateTime readAt)
{
    if (Status != NotificationStatus.Read)
    {
        Status = NotificationStatus.Read;
        ReadAt = readAt;
        ModifiedAt = readAt;
    }
}
```

- **Idempotent:** Calling `MarkAsRead` on an already-read notification is a no-op
- **No transition from Archived/Deleted:** Only transitions from non-Read states
- **Uses IClock:** Both single and bulk mark-as-read use `IClock.UtcNow` for testability
