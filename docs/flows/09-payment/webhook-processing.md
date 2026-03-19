# Webhook Processing (Background Jobs)

## Tong quan

He thong su dung 2 background job chinh de xu ly webhook va doi soat giao dich: ProcessGatewayWebhooksJob (xu ly webhook VNPay) va GatewayReconciliationJob (doi soat giao dich Pending qua han).

## Background Jobs

### 1. ProcessGatewayWebhooksJob

- **Scheduler:** Quartz.NET
- **Interval:** 10 giay
- **Batch size:** 50 webhook/lan
- **Concurrency:** `[DisallowConcurrentExecution]`

#### Flow xu ly:
1. Lay toi da 50 `GatewayWebhookEvent` co `Status = Pending`
2. Voi moi webhook:
   - Parse lai query params
   - Goi `ProcessVnPayCallbackCommand` de xu ly
   - Cap nhat status cua webhook: `Processed` hoac `Failed`
3. Retry: neu exception, job duoc refire ngay lap tuc

#### IPN vs Webhook:
- IPN endpoint (`GET /api/payments/vnpay/ipn`) nhan va luu webhook
- Job nay doc tu DB va xu ly bat dong bo
- Dam bao khong mat du lieu ngay ca khi xu ly that bai

### 2. GatewayReconciliationJob

- **Scheduler:** Quartz.NET
- **Interval:** 15 phut
- **Batch size:** 100 transaction/lan
- **Concurrency:** `[DisallowConcurrentExecution]`

#### Flow xu ly:
1. Tim cac Transaction co `Status = Pending` qua han (tao truoc thoi diem cutoff)
2. Voi moi transaction:
   - Goi VNPay Query API de kiem tra trang thai thuc te
   - Neu VNPay confirm thanh cong: xu ly nhu callback binh thuong
   - Neu VNPay confirm that bai: danh dau `Failed`
   - Neu VNPay khong co thong tin: giu `Pending` de kiem tra lan sau
3. Luu ket qua vao DB

## Architecture

```
VNPay -> IPN Endpoint -> Luu GatewayWebhookEvent -> DB
                                                      |
ProcessGatewayWebhooksJob (10s) -> Doc tu DB -> ProcessVnPayCallbackCommand
                                                      |
                                               HandleDepositAsync
                                               HandleOrderPaymentAsync
                                               HandleBuyNowAsync
                                               HandleWalletTopUpAsync

GatewayReconciliationJob (15min) -> Query VNPay API -> Cap nhat Transaction
```

## Idempotency

- Callback xu ly idempotent: kiem tra `Transaction.Status` truoc khi xu ly
- Neu Transaction da `Completed` hoac `Failed`: bo qua, tra ve ket qua cu
- Webhook event co the duoc xu ly nhieu lan ma khong gay loi

## Luu y nghiep vu

- ProcessGatewayWebhooksJob chay moi 10 giay de dam bao xu ly nhanh
- GatewayReconciliationJob la mang luoi an toan cho cac giao dich bi mat callback
- Tat ca giao dich tai chinh deu co audit log tu TransactionCompletedAuditHandler va TransactionFailedAuditHandler
- He thong uu tien xu ly IPN callback truoc Return callback
