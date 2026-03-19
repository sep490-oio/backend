# 13 - Notification

## Tong quan

Module notification xu ly toan bo luong thong bao trong he thong OIO: tu tao notification tu domain events, den delivery qua nhieu kenh (in-app, email, SignalR), va quan ly trang thai doc.

## Thanh phan chinh

| Thanh phan | Mo ta |
|---|---|
| **Notification** | Aggregate chinh - chua noi dung thong bao |
| **NotificationDelivery** | Theo doi viec gui thong bao qua tung kenh |
| **INotificationProvider** | Interface cho cac kenh gui thong bao |
| **NotificationHub** | SignalR hub cho real-time notifications |

## Cac subflow

| File | Mo ta |
|---|---|
| [event-to-notification.md](./event-to-notification.md) | Chuyen domain event thanh notification |
| [delivery-job.md](./delivery-job.md) | Background job gui thong bao |
| [channels.md](./channels.md) | Cac kenh gui thong bao |
| [read-management.md](./read-management.md) | Quan ly trang thai doc/chua doc |

## Notification Types

| Type | Mo ta | Event Types |
|---|---|---|
| `auction` | Dau gia | `auction_won`, `auction_sold`, `auction_ended`, `bid_placed`, `outbid` |
| `order` | Don hang | `order_cancelled`, `order_shipped`, `order_delivered`, `decision_window_started` |
| `financial` | Tai chinh | `wallet_credited`, `wallet_debited`, `transaction_failed`, `withdrawal_completed`, `withdrawal_rejected`, `invoice_paid`, `escrow_released`, `escrow_refunded` |
| `moderation` | Kiem duyet | `item_approved`, `item_rejected` |

## Endpoints

| Method | URL | Mo ta |
|---|---|---|
| `GET` | `/api/notifications` | Danh sach thong bao (phan trang) |
| `GET` | `/api/notifications/unread-count` | So thong bao chua doc |
| `PATCH` | `/api/notifications/{notificationId}/read` | Danh dau da doc |
| `PATCH` | `/api/notifications/read-all` | Danh dau tat ca da doc |

## Background Jobs

| Job | Interval | Mo ta |
|---|---|---|
| `ProcessNotificationDeliveriesJob` | Quartz (cau hinh) | Gui thong bao qua cac kenh |
