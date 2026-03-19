# Xu Ly Callback Dat Coc Dau Gia

## Tong quan

Khi user thanh toan dat coc (AuctionDeposit) thanh cong qua VNPay, he thong nap tien vao wallet, hold so tien dat coc, tao AuctionDeposit record, va dang ky user thanh participant cua auction.

## Actors

- **Buyer** - nguoi dat coc
- **System** - xu ly callback tu dong
- **VNPay** - gui ket qua thanh toan

## Endpoint Sequence

### Step 1: VNPay Return (redirect user)
- **Method:** `GET /api/payments/vnpay/return`
- **Auth:** Anonymous (user duoc redirect tu VNPay)
- **Response:** `200 OK`
  ```json
  {
    "transactionRef": "20260319...",
    "isSuccess": true,
    "responseCode": "00",
    "message": "Thanh toan thanh cong"
  }
  ```

## Business Logic Chi Tiet (HandleAuctionDepositAsync)

### Buoc 1: Validate
- Transaction phai co `AuctionId`
- Auction phai ton tai va co trang thai hop le
- Seller khong the dat coc cho auction cua minh
- Qualification Window phai dang mo
- User chua co deposit dang Hold

### Buoc 2: Nap tien vao Wallet
- `wallet.Credit(amount, transactionId, description, nowUtc)`
- Tang `AvailableBalance` cua wallet

### Buoc 3: Hold tien dat coc
- `wallet.Hold(amount, transactionId, description, nowUtc)`
- Chuyen tien tu `AvailableBalance` sang `PendingBalance`
- Tien bi hold khong the rut ra

### Buoc 4: Tao AuctionDeposit
- `AuctionDeposit.Create(auctionId, bidderId, amount, transactionId, nowUtc)`
- Trang thai `IsHeld = true`

### Buoc 5: Dang ky Participant
- `auction.RegisterParticipantFromDeposit(userId, nowUtc)`
- User chinh thuc tham gia dau gia

### Buoc 6: Hoan thanh Transaction
- `transaction.MarkAsCompleted(gatewayInfo, nowUtc)`
- Auto-create/link PaymentMethod tu VNPay token (neu co)

## State Machine

```
Buyer -> Tao URL (Pending) -> VNPay thanh toan -> Callback
  -> Wallet.Credit -> Wallet.Hold -> AuctionDeposit.Create
  -> Participant.Register -> Transaction.Completed
```

## Domain Events & Side Effects

- `TransactionCompletedDomainEvent` -> Audit log
- `WalletCreditedDomainEvent` -> Notification "Vi du duoc cong tien"
- User tro thanh participant cua auction, co the dat gia

## Luu y nghiep vu

- Tien dat coc bi hold trong wallet, khong phai chuyen di cho khac
- Khi auction ket thuc, deposit duoc return (unhold) hoac convert thanh payment
- Neu user da co deposit dang Hold -> tra ve loi Conflict
- Qualification Window phai dang mo moi cho phep dat coc
