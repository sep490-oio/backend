# 14 - Wallet & Withdrawal

## Tong quan

Module quan ly vi dien tu va rut tien. Wallet la trung tam tai chinh cua user, ho tro cac thao tac: nap tien (credit), tru tien (debit), tam giu (hold), giai phong (unhold), va rut tien ve tai khoan ngan hang.

## Wallet Operations

| Operation | Mo ta |
|---|---|
| `Credit` | Nap tien vao vi (tang AvailableBalance) |
| `Debit` | Tru tien tu vi (giam AvailableBalance) |
| `Hold` | Tam giu tien (chuyen tu Available sang Pending) |
| `DebitPending` | Tru tien tu so du tam giu |
| `Unhold` | Giai phong tien tam giu (chuyen tu Pending sang Available) |

## Cac subflow

| File | Mo ta |
|---|---|
| [wallet-topup.md](./wallet-topup.md) | Nap tien vao vi |
| [hold-unhold.md](./hold-unhold.md) | Tam giu va giai phong tien |
| [view-transactions.md](./view-transactions.md) | Xem lich su giao dich |
| [withdrawal.md](./withdrawal.md) | Rut tien ve ngan hang |

## Endpoints

### Wallet
| Method | URL | Mo ta |
|---|---|---|
| `GET` | `/api/me/wallet` | Xem so du vi |
| `GET` | `/api/me/wallet/transactions` | Lich su giao dich |
| `GET` | `/api/me/wallet/transactions/{transactionId}` | Chi tiet giao dich |

### Withdrawal
| Method | URL | Mo ta |
|---|---|---|
| `POST` | `/api/me/wallet/withdrawals` | Tao yeu cau rut tien |
| `GET` | `/api/me/wallet/withdrawals` | Danh sach yeu cau rut tien |
| `POST` | `/api/me/wallet/withdrawals/{withdrawalId}/cancel` | Huy yeu cau rut tien |

### Admin
| Method | URL | Mo ta |
|---|---|---|
| `GET` | `/api/admin/payments/withdrawals` | Tat ca yeu cau rut tien |
| `GET` | `/api/admin/payments/withdrawals/{withdrawalId}` | Chi tiet yeu cau |
| `POST` | `/api/admin/payments/withdrawals/{withdrawalId}/approve` | Duyet rut tien |
| `POST` | `/api/admin/payments/withdrawals/{withdrawalId}/reject` | Tu choi rut tien |

## Domain Events

- `WalletCreditedDomainEvent` -> Notification "Vi du duoc cong tien"
- `WalletDebitedDomainEvent` -> Notification "Vi du bi tru tien"
- `WithdrawalApprovedDomainEvent` -> Audit log
- `WithdrawalCompletedDomainEvent` -> Notification "Rut tien thanh cong"
- `WithdrawalRejectedDomainEvent` -> Audit log + Notification
