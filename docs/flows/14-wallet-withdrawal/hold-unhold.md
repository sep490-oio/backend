# Tam Giu & Giai Phong Tien (Hold / Unhold)

## Tong quan

He thong tam giu tien (hold) trong wallet khi user dat coc dau gia. Tien bi hold khong the rut ra hoac dung cho muc dich khac. Khi auction ket thuc, tien duoc giai phong (unhold) hoac chuyen thanh payment.

## Actors

- **System** - tu dong hold/unhold tu domain events

## Hold Flow

### Khi nao tien bi Hold?
1. **Dat coc dau gia (AuctionDeposit):**
   - Sau khi VNPay callback thanh cong cho AuctionDeposit
   - `wallet.Credit(amount)` -> `wallet.Hold(amount)`
   - Tien chuyen tu AvailableBalance sang PendingBalance

2. **Withdrawal Request:**
   - Khi user tao yeu cau rut tien
   - Tien bi hold cho den khi admin approve/reject

## Unhold Flow

### Khi nao tien duoc Unhold?

1. **Auction ket thuc - user khong thang:**
   - Domain event: `AuctionEndedEvent` (loser deposits)
   - Handler: `AuctionDepositReleaseEventHandlers`
   - `wallet.Unhold(amount)` -> tien chuyen lai AvailableBalance
   - `deposit.Release(now)` -> deposit chuyen sang Released

2. **Auction bi huy:**
   - Domain event: `AuctionCancelledEvent`
   - Handler: `AuctionCancelledEventHandler`
   - Tat ca deposit duoc release, tien unhold cho tat ca participant

3. **Withdrawal bi reject:**
   - Admin reject withdrawal
   - Tien bi hold duoc tra lai AvailableBalance

## DebitPending Flow

### Khi nao tien bi DebitPending?

1. **Winner thanh toan don hang:**
   - Deposit cua winner duoc convert thanh payment
   - `wallet.DebitPending(depositAmount)` -> tru tien tu PendingBalance
   - Deposit khong con hold nua, da ap dung vao thanh toan

2. **Buy-now deposit applied:**
   - Deposit cua buyer duoc ap dung vao buy-now payment
   - `wallet.DebitPending(depositAppliedAmount)`

## Wallet Balance

```
TotalBalance = AvailableBalance + PendingBalance

Credit  -> Tang AvailableBalance
Debit   -> Giam AvailableBalance
Hold    -> Giam AvailableBalance, Tang PendingBalance
Unhold  -> Tang AvailableBalance, Giam PendingBalance
DebitPending -> Giam PendingBalance
```

## Luu y nghiep vu

- Tien bi hold van nam trong wallet cua user nhung khong the su dung
- Hold/Unhold la thao tac noi bo, khong co endpoint truc tiep
- Moi thao tac wallet tao WalletTransaction de audit
- PendingBalance co the am trong truong hop loi (he thong can xu ly)
