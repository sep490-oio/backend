# Chuyen Domain Event Thanh Notification

## Tong quan

He thong su dung MediatR INotificationHandler de lang nghe domain events va tu dong tao notification. Moi event type co handler rieng, dam bao noi dung thong bao phu hop.

## Actors

- **System** - tu dong tao notification tu domain events

## Architecture

```
Domain Event -> Outbox -> MediatR Pipeline -> INotificationHandler
                                                    |
                                            CreateNotificationCommand
                                                    |
                                            Notification.Create() + NotificationDelivery
```

## CreateNotificationCommand

```csharp
CreateNotificationCommand(
    UserId: Guid,             // Nguoi nhan
    NotificationType: string, // "auction" | "order" | "financial" | "moderation"
    EventType: string,        // "auction_won" | "order_shipped" | ...
    Title: string,            // Tieu de
    Message: string,          // Noi dung chi tiet
    Priority: NotificationPriority?, // Normal | High | Urgent
    EntityType: string?,      // "Auction" | "Order" | "Wallet" | ...
    EntityId: Guid?,          // ID cua entity lien quan
    Metadata: string?,        // JSON chua du lieu bo sung
    RelatedEntities: string?, // JSON chua cac entity lien quan
    Actions: string?,         // JSON chua cac action button
    ExpiresAt: DateTime?      // Thoi gian het han
)
```

## Danh sach Event Handlers

### Auction Events
| Event | Handler | Notification |
|---|---|---|
| `AuctionSoldEvent` | `AuctionSoldEventHandler` | Winner: "Ban da thang dau gia", Seller: "Phien dau gia da co nguoi thang", Watchers: "Phien dau gia da ket thuc" |
| `AuctionEndedEvent` | `AuctionEndedEventHandler` | Broadcast qua SignalR |
| `BidPlacedEvent` | `BidPlacedEventHandler` | Real-time broadcast |
| `OutbidEvent` | `OutbidEventHandler` | "Ban da bi vuot gia" |

### Order Events
| Event | Handler | Notification |
|---|---|---|
| `OrderCancelledEvent` | `OrderCancelledNotificationHandler` | "Don hang da bi huy. Ly do: {reason}" |
| `OutboundShipmentPickedUpEvent` | `OrderShippedNotificationHandler` | "Don hang da duoc giao don vi van chuyen" |
| `OutboundShipmentDeliveredEvent` | `OrderDeliveredNotificationHandler` | "Don hang da duoc giao thanh cong" + "Thoi gian quyet dinh da bat dau" |

### Financial Events
| Event | Handler | Notification |
|---|---|---|
| `WalletCreditedDomainEvent` | `WalletCreditedNotificationHandler` | "Vi du duoc cong tien. So du: {balance}" |
| `WalletDebitedDomainEvent` | `WalletDebitedNotificationHandler` | "Vi du bi tru tien. So du: {balance}" |
| `TransactionFailedDomainEvent` | `TransactionFailedNotificationHandler` | "Giao dich that bai" (Priority: High) |
| `WithdrawalCompletedDomainEvent` | `WithdrawalCompletedNotificationHandler` | "Rut tien thanh cong" |
| `WithdrawalRejectedDomainEvent` | `WithdrawalRejectedNotificationHandler` | "Yeu cau rut tien bi tu choi" (Priority: High) |
| `InvoicePaidDomainEvent` | `InvoicePaidNotificationHandler` | "Thanh toan thanh cong" |
| `EscrowReleasedToSellerDomainEvent` | `EscrowReleasedNotificationHandler` | "Tien giu da duoc giai ngan" (Priority: High) |
| `EscrowRefundedToBuyerDomainEvent` | `EscrowRefundedNotificationHandler` | "Tien giu da duoc hoan" (Priority: High) |

### Moderation Events
| Event | Handler | Notification |
|---|---|---|
| `AuctionApprovedEvent` | `AuctionApprovedNotificationHandler` | "San pham da duoc phe duyet" |
| `AuctionRejectedEvent` | `AuctionRejectedNotificationHandler` | "San pham can chinh sua" (Priority: High) |

## NotificationDispatch Helper

- `NotificationDispatch.DispatchAsync(sender, logger, command, ct)` - Gui command voi error handling
- `NotificationDispatch.FormatAmount(amount, currency?)` - Format so tien
- `NotificationDispatch.SerializeMetadata(object)` - Serialize metadata JSON

## Luu y nghiep vu

- Notification duoc tao bat dong bo qua Outbox pattern
- Moi notification co the co nhieu delivery (1 per kenh)
- Metadata JSON cho phep frontend hien thi noi dung phong phu
- Actions JSON cho phep frontend hien thi action button (vi du: "Thanh toan ngay")
- High priority notifications duoc xu ly uu tien
