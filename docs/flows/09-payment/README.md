# 09 - Payment

## Tong quan

Module thanh toan xu ly toan bo luong tai chinh cua he thong OIO, bao gom tao URL thanh toan VNPay, xu ly callback (IPN + Return), nap vi, thanh toan don hang, mua ngay (Buy Now), hoan tien, va quan ly phuong thuc thanh toan (token card).

## Thanh phan chinh

| Thanh phan | Mo ta |
|---|---|
| **VNPay Gateway** | Tich hop cong thanh toan VNPay (pay, pay_and_create, token_pay, refund) |
| **Transaction** | Aggregate theo doi trang thai giao dich: Pending -> Completed / Failed / Refunded |
| **Wallet** | Vi dien tu: Credit, Debit, Hold, DebitPending |
| **Escrow** | Giu tien cho don hang cho den khi giao thanh cong |
| **PaymentMethod** | Luu tru token the VNPay de thanh toan nhanh |

## PaymentPurpose

- `AuctionDeposit` - Dat coc tham gia dau gia
- `OrderPayment` - Thanh toan don hang
- `AuctionBuyNow` - Mua ngay
- `WalletTopUp` - Nap tien vao vi

## Cac subflow

| File | Mo ta |
|---|---|
| [create-payment-url.md](./create-payment-url.md) | Tao URL thanh toan VNPay (flow chinh) |
| [token-payment.md](./token-payment.md) | Thanh toan bang token the da luu |
| [ipn-callback.md](./ipn-callback.md) | Xu ly IPN callback tu VNPay (server-to-server) |
| [deposit-callback.md](./deposit-callback.md) | Xu ly callback dat coc dau gia |
| [order-payment-callback.md](./order-payment-callback.md) | Xu ly callback thanh toan don hang |
| [buy-now-callback.md](./buy-now-callback.md) | Xu ly callback mua ngay |
| [wallet-topup-callback.md](./wallet-topup-callback.md) | Xu ly callback nap vi |
| [refund.md](./refund.md) | Hoan tien qua VNPay |
| [webhook-processing.md](./webhook-processing.md) | Background job xu ly webhook |

## Background Jobs

| Job | Interval | Mo ta |
|---|---|---|
| `ProcessGatewayWebhooksJob` | 10 giay | Xu ly webhook VNPay da luu trong DB |
| `GatewayReconciliationJob` | 15 phut | Doi soat giao dich Pending qua han |

## Domain Events

- `TransactionCompletedDomainEvent` -> Audit log + Notification
- `TransactionFailedDomainEvent` -> Audit log + Notification
- `WalletCreditedDomainEvent` -> Notification "Vi du duoc cong tien"
- `WalletDebitedDomainEvent` -> Notification "Vi du bi tru tien"
- `WithdrawalApprovedDomainEvent` -> Audit log
- `WithdrawalRejectedDomainEvent` -> Audit log + Notification
- `WithdrawalCompletedDomainEvent` -> Notification
- `EscrowReleasedToSellerDomainEvent` -> Audit log + Notification
- `EscrowRefundedToBuyerDomainEvent` -> Audit log + Notification
- `InvoicePaidDomainEvent` -> Notification
