# Cac Kenh Gui Thong Bao (Channels)

## Tong quan

He thong ho tro nhieu kenh gui thong bao qua interface `INotificationProvider`. Moi provider implement logic gui cu the cho kenh cua minh.

## INotificationProvider Interface

```csharp
interface INotificationProvider
{
    string ChannelType { get; }
    Task<Result> SendAsync(
        Notification notification,
        NotificationDelivery delivery,
        CancellationToken ct);
}
```

## Cac kenh hien co

### 1. In-App (Database)
- **ChannelType:** `in_app`
- Luu notification trong DB, frontend query qua API
- Real-time push qua SignalR hub

### 2. Email
- **ChannelType:** `email`
- Gui email thong bao qua `IEmailSender`
- Template email theo `NotificationType` va `EventType`

### 3. SignalR (Real-time)
- Hub cho notification real-time
- User connect khi dang nhap
- Push notification ngay khi tao (khong qua delivery job)

## SignalR Hubs

### NotificationHub
- Gui notification real-time cho user dang online
- User tu dong join group theo UserId

### AuctionHub
- Gui update dau gia real-time (bid, outbid, auction ended, ...)
- User join room theo AuctionId

### DisputeHub
- Gui tin nhan dispute real-time
- User join room theo DisputeId

## Channel Selection

- Moi notification type co danh sach kenh mac dinh
- High priority notification duoc gui qua tat ca kenh
- Normal priority chi gui in-app + SignalR

## Luu y nghiep vu

- SignalR notification la real-time, khong qua delivery job
- Email delivery co the cham do SMTP
- In-app notification luon duoc tao (du co kenh khac)
- Provider khong tim thay cho channel se bi danh dau `NO_PROVIDER`
