# OIO Auction Platform - Flow Documentation

Tai lieu mo ta chi tiet cac luong nghiep vu (business flows) cua he thong dau gia OIO.
Moi flow bao gom: endpoint sequence, request/response, business rules, domain events va side effects.

## Muc luc

### 01 - Registration & Authentication
Luong dang ky, xac thuc va quan ly phien dang nhap.

- [Tong quan](./01-registration-auth/README.md)
- [Dang ky tai khoan](./01-registration-auth/registration.md)
- [Xac thuc email](./01-registration-auth/email-verification.md)
- [Dang nhap](./01-registration-auth/login.md)
- [Lam moi token](./01-registration-auth/refresh-token.md)
- [Quen & dat lai mat khau](./01-registration-auth/forgot-reset-password.md)
- [Xac thuc hai yeu to - TOTP 2FA](./01-registration-auth/two-factor-auth.md) (Setup, Confirm, Login verify, Recovery codes)
- [Quan ly phien dang nhap](./01-registration-auth/session-management.md)

### 02 - User Profile
Quan ly thong tin ca nhan, so dien thoai, dia chi cua nguoi dung.

- [Tong quan](./02-user-profile/README.md)
- [Cap nhat ho so](./02-user-profile/update-profile.md)
- [Xac thuc so dien thoai](./02-user-profile/phone-verification.md)
- [Quan ly dia chi](./02-user-profile/address-management.md)
- [Ho so nguoi ban](./02-user-profile/seller-profile.md)

### 03 - Seller Verification (eKYC)
Luong xac minh danh tinh nguoi ban (Identity Verification / eKYC).

- [Tong quan](./03-seller-verification/README.md)
- [Tao yeu cau xac minh](./03-seller-verification/create-verification.md)
- [Upload tai lieu](./03-seller-verification/upload-documents.md)
- [Gui eKYC](./03-seller-verification/submit-ekyc.md)
- [Admin duyet](./03-seller-verification/admin-review.md)
- [Khieu nai / Sua thong tin](./03-seller-verification/correction-dispute.md)

### 04 - Media Upload
Luong upload media (hinh anh, video) qua Cloudinary voi signed upload.

- [Tong quan](./04-media-upload/README.md)
- [Yeu cau chu ky upload](./04-media-upload/request-signature.md)
- [Client upload len Cloudinary](./04-media-upload/client-upload.md)
- [Xac nhan upload](./04-media-upload/confirm-upload.md)
- [Background relocation & cleanup](./04-media-upload/background-relocation-cleanup.md)

### 05 - Item Management
Quan ly san pham dau gia: tao, gan media, gui duyet, admin review, kich hoat.

- [Tong quan](./05-item-management/README.md)
- [Tao san pham](./05-item-management/create-item.md)
- [Quan ly media cua san pham](./05-item-management/manage-media.md)
- [Gui duyet san pham](./05-item-management/submit-for-review.md)
- [Admin duyet san pham](./05-item-management/admin-review.md)
- [Kich hoat san pham](./05-item-management/activate-item.md)
- [Giao hang san pham](./05-item-management/item-shipping.md)
- [Kiem tra chat luong (QA)](./05-item-management/item-qa.md)

### 06 - Auction Lifecycle
Vong doi auction: tao, cau hinh, gui duyet, publish, activation, auto-extension, ket thuc, huy, relist.

- [Tong quan](./06-auction-lifecycle/README.md)
- [Tao auction](./06-auction-lifecycle/create-auction.md)
- [Cau hinh timing/pricing](./06-auction-lifecycle/set-timing-pricing.md)
- [Submit & Publish](./06-auction-lifecycle/submit-publish.md)
- [Activation](./06-auction-lifecycle/activation.md)
- [Auto-extension](./06-auction-lifecycle/auto-extension.md)
- [Ket thuc & Resolve](./06-auction-lifecycle/end-resolve.md)
- [Huy auction](./06-auction-lifecycle/cancel-auction.md)
- [Relist auction](./06-auction-lifecycle/relist-auction.md)
- [Runner-up offer](./06-auction-lifecycle/runner-up-offer.md)

### 07 - Bidding
Dat gia, auto-bid, sealed bid, deposit qualification, realtime qua SignalR.

- [Tong quan](./07-bidding/README.md)
- [Deposit & qualification](./07-bidding/deposit-qualification.md)
- [Manual bid](./07-bidding/manual-bid.md)
- [Auto-bid configure](./07-bidding/auto-bid-configure.md)
- [Auto-bid battle](./07-bidding/auto-bid-battle.md)
- [Auto-bid pause/resume](./07-bidding/auto-bid-pause-resume.md)
- [Sealed bid](./07-bidding/sealed-bid.md)
- [Watch auction](./07-bidding/watch-auction.md)
- [Invalid bid detection](./07-bidding/invalid-bid-detection.md)

### 08 - Buy Now
Mua ngay: reserve, thanh toan, finalize, het han.

- [Tong quan](./08-buy-now/README.md)
- [Khoi tao reservation](./08-buy-now/initiate-reservation.md)
- [Thanh toan](./08-buy-now/payment.md)
- [Finalize](./08-buy-now/finalize.md)
- [Thanh toan tre](./08-buy-now/late-payment.md)
- [Het han reservation](./08-buy-now/reservation-expiry.md)

### 09 - Payment
Tich hop VNPay: tao URL, IPN callback, deposit, order payment, wallet topup, refund.

- [Tong quan](./09-payment/README.md)
- [Tao payment URL](./09-payment/create-payment-url.md)
- [Token payment](./09-payment/token-payment.md)
- [IPN callback](./09-payment/ipn-callback.md)
- [Deposit callback](./09-payment/deposit-callback.md)
- [Order payment callback](./09-payment/order-payment-callback.md)
- [Buy-now callback](./09-payment/buy-now-callback.md)
- [Wallet topup callback](./09-payment/wallet-topup-callback.md)
- [Refund](./09-payment/refund.md)
- [Webhook processing](./09-payment/webhook-processing.md)

### 10 - Order Lifecycle
Vong doi order: tu dong tao tu auction, thanh toan, giao hang, return, hoan tat.

- [Tong quan](./10-order-lifecycle/README.md)
- [Tu dong tao tu auction](./10-order-lifecycle/auto-create-from-auction.md)
- [Checkout payment](./10-order-lifecycle/checkout-payment.md)
- [Huy order het han](./10-order-lifecycle/cancel-expired.md)
- [Payment default](./10-order-lifecycle/payment-default.md)
- [Shipped & delivered](./10-order-lifecycle/shipped-delivered.md)
- [Decision window](./10-order-lifecycle/decision-window.md)
- [Return flow](./10-order-lifecycle/return-flow.md)
- [Auto complete](./10-order-lifecycle/auto-complete.md)

### 11 - Warehouse & Shipping
Kho hang va van chuyen: inbound, inspection, storage, outbound, GHN tracking.

- [Tong quan](./11-warehouse-shipping/README.md)
- [Inbound shipment](./11-warehouse-shipping/inbound-shipment.md)
- [Inspection](./11-warehouse-shipping/inspection.md)
- [**Inspector workflow (end-to-end)**](./11-warehouse-shipping/inspector-workflow.md)
- [Storage](./11-warehouse-shipping/storage.md)
- [Outbound shipment](./11-warehouse-shipping/outbound-shipment.md)
- [GHN tracking](./11-warehouse-shipping/ghn-tracking.md)
- [Cancel shipment](./11-warehouse-shipping/cancel-shipment.md)

### 12 - Dispute & Moderation
Bao cao, khieu nai, xu ly tranh chap, emergency auction.

- [Tong quan](./12-dispute-moderation/README.md)
- [Tao report](./12-dispute-moderation/create-report.md)
- [Admin quan ly report](./12-dispute-moderation/admin-report-management.md)
- [Dispute chat](./12-dispute-moderation/dispute-chat.md)
- [Admin giai quyet dispute](./12-dispute-moderation/admin-resolve-dispute.md)
- [Auction emergency](./12-dispute-moderation/auction-emergency.md)

### 13 - Notification
He thong thong bao: event → notification → delivery qua nhieu kenh.

- [Tong quan](./13-notification/README.md)
- [Event to notification](./13-notification/event-to-notification.md)
- [Delivery job](./13-notification/delivery-job.md)
- [Channels](./13-notification/channels.md)
- [Read management](./13-notification/read-management.md)

### 14 - Wallet & Withdrawal
Vi dien tu: nap tien, hold/unhold, rut tien, lich su giao dich.

- [Tong quan](./14-wallet-withdrawal/README.md)
- [Wallet topup](./14-wallet-withdrawal/wallet-topup.md)
- [Hold/unhold](./14-wallet-withdrawal/hold-unhold.md)
- [Xem giao dich](./14-wallet-withdrawal/view-transactions.md)
- [Rut tien](./14-wallet-withdrawal/withdrawal.md)

### 15 - Admin Operations
Cac thao tac admin: quan ly user, role, item review, auction, monitoring, terms.

- [Tong quan](./15-admin-operations/README.md)
- [Quan ly user](./15-admin-operations/user-management.md)
- [Role & permission](./15-admin-operations/role-permission.md)
- [Item review](./15-admin-operations/item-review.md)
- [Auction curation](./15-admin-operations/auction-curation.md)
- [Auction emergency](./15-admin-operations/auction-emergency.md)
- [Sealed bid reveal](./15-admin-operations/sealed-bid-reveal.md)
- [Monitoring alerts](./15-admin-operations/monitoring-alerts.md)
- [Risk flags](./15-admin-operations/risk-flags.md)
- [Terms management](./15-admin-operations/terms-management.md)
- [Payment admin](./15-admin-operations/payment-admin.md)

---

## Quy uoc

| Ky hieu | Y nghia |
|---------|---------|
| `Required` | Bat buoc |
| `Optional` | Tuy chon |
| `Auth: Required` | Can dang nhap (Bearer token) |
| `Auth: Anonymous` | Khong can dang nhap |
| `Permission: X` | Can quyen X |

## Kien truc tong quan

```
Client (SPA / Mobile)
    |
    v
[API Gateway / Endpoints]  -->  [MediatR Pipeline]  -->  [Command/Query Handlers]
    |                              |                           |
    |                         Validation                  Domain Logic
    |                         Behavior                    (Aggregates, VOs, Events)
    |                              |                           |
    |                              v                           v
    |                        [Domain Events]           [EF Core / DbContext]
    |                              |
    |                              v
    |                   [Event Handlers / Side Effects]
    |                   (Email, Notification, Jobs...)
    v
[Background Jobs]
  - ExpiredSessionCleanupJob (moi 6h)
  - PendingUploadRelocationJob (Quartz)
  - ScanActiveAuctionsForCollusionJob
```
