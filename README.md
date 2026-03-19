# OIO

OIO la backend cho nen tang dau gia truc tuyen, gom cac module lon: user/auth, catalog item, auction realtime, payment-wallet-order, moderation/dispute/report, warehouse va notification.

## Kien truc

- src/core/OIO.Domain: domain model, aggregate, enum, value object
- src/core/OIO.Application: command/query, DTO, service, event handler
- src/infrastructure/OIO.Infrastructure: persistence, provider, settings, integration
- src/presentation/OIO.Api: HTTP API, SignalR hub, OpenAPI/Scalar

## Chay local

1. Cap nhat .env neu can.
2. Khoi dong dependency bang compose.yaml va compose.override.yaml.
3. Chay API tu src/presentation/OIO.Api.
4. Trong development, OpenAPI duoc map tai /openapi/v1.json va Scalar tai /docs.

## Auth conventions

- API mac dinh dung Bearer JWT.
- Route api/admin/* danh cho admin.
- Mot so route POST duoc gate boi Idempotency-Key.
- SignalR hubs deu can auth va co them permission/check o tung method khi can.

### Two-Factor Authentication (TOTP)

He thong ho tro 2FA bang TOTP (Time-based One-Time Password), tuong thich Google Authenticator / Authy.

**Flow:**
1. `POST /api/me/two-factor/setup` — Tao TOTP secret + QR code
2. `POST /api/me/two-factor/confirm` — Xac nhan bang ma 6 so → nhan 8 recovery codes
3. Login: `POST /api/auth/login` tra ve limited JWT (3 phut) khi 2FA bat
4. `POST /api/auth/two-factor/verify` — Xac thuc TOTP/recovery code → nhan full JWT

**Packages:** Otp.NET, QRCoder.
**Chi tiet:** [docs/flows/01-registration-auth/two-factor-auth.md](./docs/flows/01-registration-auth/two-factor-auth.md)

## Tai lieu API

- [API index](./docs/api/README.md)
- [User + Auth](./docs/api/user.md)
- [Auction + Catalog](./docs/api/auction.md)
- [Payment + Order](./docs/api/payment-order.md)
- [Moderation + Warehouse](./docs/api/moderation-warehouse.md)
- [SignalR](./docs/api/signalr.md)
- [Schemas appendix](./docs/api/schemas.md)

## Tai lieu Business Flows

Tai lieu mo ta chi tiet cac luong nghiep vu cua he thong dau gia OIO. Moi flow bao gom endpoint sequence, request/response, business rules, domain events va side effects.

- [Flow index](./docs/flows/README.md)
- [01 - Registration & Authentication](./docs/flows/01-registration-auth/README.md)
- [02 - User Profile](./docs/flows/02-user-profile/README.md)
- [03 - Seller Verification](./docs/flows/03-seller-verification/README.md)
- [04 - Media Upload](./docs/flows/04-media-upload/README.md)
- [05 - Item Management](./docs/flows/05-item-management/README.md)
- [06 - Auction Lifecycle](./docs/flows/06-auction-lifecycle/README.md)
- [07 - Bidding](./docs/flows/07-bidding/README.md)
- [08 - Buy Now](./docs/flows/08-buy-now/README.md)
- [09 - Payment](./docs/flows/09-payment/README.md)
- [10 - Order Lifecycle](./docs/flows/10-order-lifecycle/README.md)
- [11 - Warehouse Shipping](./docs/flows/11-warehouse-shipping/README.md)
- [12 - Dispute Moderation](./docs/flows/12-dispute-moderation/README.md)
- [13 - Notification](./docs/flows/13-notification/README.md)
- [14 - Wallet Withdrawal](./docs/flows/14-wallet-withdrawal/README.md)
- [15 - Admin Operations](./docs/flows/15-admin-operations/README.md)

## Quy uoc Flow Docs

| Ky hieu | Y nghia |
|---------|---------|
| `Required` | Bat buoc |
| `Optional` | Tuy chon |
| `Auth: Required` | Can dang nhap (Bearer token) |
| `Auth: Anonymous` | Khong can dang nhap |
| `Permission: X` | Can quyen X |

## Kien truc luong nghiep vu

```text
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

## Regenerate docs

- Chay scripts/generate-api-docs.ps1 de regenerate docs tu source hien tai.
- Lan dau hoac khi can bootstrap descriptions.yaml, chay scripts/generate-api-docs.ps1 -BootstrapDescriptions.
