# VNPay IPN Callback

## Tong quan

VNPay IPN (Instant Payment Notification) la callback server-to-server. VNPay goi endpoint nay de thong bao ket qua thanh toan. He thong validate signature, luu webhook event vao DB, va xu ly bat dong bo qua background job.

## Actors

- **VNPay** - goi callback truc tiep (khong qua browser)
- **System** - validate, luu webhook, xu ly bat dong bo

## Endpoint Sequence

### Step 1: Nhan IPN tu VNPay

- **Method:** `GET /api/payments/vnpay/ipn`
- **Auth:** Anonymous (VNPay goi truc tiep, khong co JWT)
- **Request:** Query string tu VNPay chua cac tham so:
  - `vnp_TxnRef` - Ma giao dich
  - `vnp_Amount` - So tien
  - `vnp_ResponseCode` - Ma ket qua (00 = thanh cong)
  - `vnp_SecureHash` - Chu ky bao mat
  - ... va cac tham so khac
- **Response:** `200 OK`
  ```json
  {
    "RspCode": "00",
    "Message": "Confirm Success"
  }
  ```
- **Ghi chu:**
  - Luon tra ve JSON voi `RspCode`
  - `"00"` = da nhan thanh cong
  - `"97"` = signature khong hop le hoac loi xu ly

## Business Logic Chi Tiet

### Buoc 1: Validate Signature
- Parse query string tu VNPay
- Goi `IPaymentGatewayService.ProcessCallback()` de validate chu ky HMAC-SHA512
- Neu signature khong hop le: tra ve `RspCode = "97"`

### Buoc 2: Luu Webhook Event
- Tao `GatewayWebhookEvent` voi:
  - `provider = "vnpay"`
  - `eventType = "ipn"`
  - `rawContent` = JSON serialize cua query params
  - `status = Pending`
- Luu vao DB

### Buoc 3: Tra ve ngay cho VNPay
- Tra ve `RspCode = "00"` ngay lap tuc
- Viec xu ly chi tiet (cap nhat wallet, order, deposit) se thuc hien trong background

## Background Jobs

### ProcessGatewayWebhooksJob
- **Interval:** 10 giay
- **Batch size:** 50 webhook/lan
- Lay cac `GatewayWebhookEvent` chua xu ly
- Goi `ProcessVnPayCallbackCommand` de xu ly tung webhook
- Cap nhat status cua webhook event

## Domain Events & Side Effects

- Webhook duoc luu de dam bao khong mat du lieu
- Xu ly bat dong bo giup VNPay nhan phan hoi nhanh (< 5 giay)
- Retry tu dong khi xu ly that bai

## Luu y nghiep vu

- VNPay co the goi IPN nhieu lan cho cung 1 giao dich -> can xu ly idempotent
- IPN va Return endpoint xu ly cung logic nhung IPN la server-to-server (dang tin cay hon)
- Khong bao gio tra ve HTTP error cho VNPay vi se gay retry vo han
- Webhook event duoc luu de co the debug va doi soat sau
