# Tu Dong Tao Order Tu Dau Gia

## Tong quan

Khi dau gia ket thuc thanh cong (co nguoi thang), he thong tu dong tao Order trong `AuctionSoldEventHandler`. Order duoc tao voi trang thai `PendingPayment` va co thoi han thanh toan 48 gio.

## Actors

- **System** - tu dong tao order tu domain event
- **Winner (Buyer)** - nguoi thang dau gia
- **Seller** - nguoi ban

## Trigger

- Domain Event: `AuctionSoldEvent`
- Handler: `AuctionSoldEventHandler`

## Business Logic Chi Tiet

### Buoc 1: Kiem tra Order da ton tai
- Tim order theo `auctionId + buyerId`
- Neu da co: return order cu (idempotent)

### Buoc 2: Tao OrderPricing
- `itemPrice` = `FinalPrice` (gia cuoi cung cua dau gia)
- `shippingFee` = 0 (tinh sau)
- `platformFee` = 0
- `taxAmount` = 0
- `totalAmount` = `FinalPrice`

### Buoc 3: Tao ShippingSnapshot
- Lay default address cua winner
- Neu khong co address: dung placeholder "Address pending update"
- Luu snapshot de co the thay doi dia chi sau

### Buoc 4: Tao Order
```
Order.Create(
    auctionId,
    buyerId: winner.Id,
    sellerId,
    shipping: shippingSnapshot,
    pricing: orderPricing,
    currency,
    paymentDueAt: occurredAt + 48h,
    notes: null | "Winner had no default address..."
)
```

### Buoc 5: Gui Notification

**Cho Winner:**
- Type: `auction_won`
- Title: "Ban da thang dau gia"
- Message: Bao gom thong tin gia cuoi va thoi han thanh toan
- Actions: `checkout_order` button de thanh toan ngay

**Cho Seller:**
- Type: `auction_sold`
- Title: "Phien dau gia da co nguoi thang"
- Message: Bao gom ten nguoi thang va gia cuoi

**Cho Watchers:**
- Type: `auction_ended`
- Title: "Phien dau gia da ket thuc"
- Chi gui cho watcher co `NotifyOnEnd = true`

### Buoc 6: Broadcast qua SignalR
- `NotifyAuctionEndedAsync` -> gui real-time toi tat ca user dang theo doi auction

## Domain Events & Side Effects

- `AuctionSoldEvent` -> Tao Order + Notifications + SignalR broadcast
- Order duoc tao voi `PaymentDueAt = OccurredAt + 48h`
- Neu winner khong co address: order van duoc tao nhung co notes canh bao

## Luu y nghiep vu

- Payment deadline la 48 gio tu khi auction ket thuc
- Order chi duoc tao 1 lan (idempotent check)
- Neu khong tao duoc Order (loi pricing, loi domain), handler log warning va return null
- Notification chua action button "Thanh toan ngay" de winner checkout nhanh
- Buy-now cung tao Order nhung trong flow rieng (buy-now-callback)
