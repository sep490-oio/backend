# Quan Ly Trang Thai Doc (Read Management)

## Tong quan

User co the xem danh sach thong bao, dem so chua doc, danh dau da doc tung thong bao hoac tat ca.

## Actors

- **User** - nguoi nhan thong bao

## Endpoint Sequence

### Xem danh sach thong bao
- **Method:** `GET /api/notifications`
- **Auth:** Required
- **Query params:** Phan trang (PageNumber, PageSize, SortColumn, SortDirection)
- **Response:** `200 OK` -> `PagedList<NotificationDto>`
- **Ghi chu:** Tra ve thong bao cua user hien tai, sap xep theo thoi gian

### Dem thong bao chua doc
- **Method:** `GET /api/notifications/unread-count`
- **Auth:** Required
- **Response:** `200 OK` -> `{ count: number }`
- **Ghi chu:** Frontend dung de hien thi badge tren icon chuong

### Danh dau da doc (1 thong bao)
- **Method:** `PATCH /api/notifications/{notificationId}/read`
- **Auth:** Required
- **Response:** `200 OK`
- **Error cases:**
  - `404 Not Found` - Notification khong ton tai hoac khong thuoc user

### Danh dau tat ca da doc
- **Method:** `PATCH /api/notifications/read-all`
- **Auth:** Required
- **Response:** `200 OK`
- **Ghi chu:** Danh dau tat ca thong bao chua doc cua user thanh da doc

## Business Logic

### GetMyNotificationsQuery
- Loc notification theo `UserId = currentUser.Id`
- Ho tro phan trang va sap xep
- Tra ve metadata (NotificationType, EventType, EntityType, EntityId, ...)

### GetUnreadNotificationCountQuery
- Dem notification co `IsRead = false` cua user

### MarkNotificationAsReadCommand
- Tim notification theo `Id` va `UserId`
- `notification.MarkAsRead(now)`

### MarkAllNotificationsAsReadCommand
- Cap nhat tat ca notification chua doc cua user

## Luu y nghiep vu

- Danh dau da doc la thao tac nhe (chi cap nhat 1 field)
- Frontend thuong goi `unread-count` dinh ky hoac khi nhan SignalR event
- `read-all` huu ich khi user muon don dep tat ca thong bao
- Notification khong bi xoa khi danh dau da doc - van co the xem lai
