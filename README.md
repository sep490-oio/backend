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

## Tai lieu API

- [API index](./docs/api/README.md)
- [User + Auth](./docs/api/user.md)
- [Auction + Catalog](./docs/api/auction.md)
- [Payment + Order](./docs/api/payment-order.md)
- [Moderation + Warehouse](./docs/api/moderation-warehouse.md)
- [SignalR](./docs/api/signalr.md)
- [Schemas appendix](./docs/api/schemas.md)

## Regenerate docs

- Chay scripts/generate-api-docs.ps1 de regenerate docs tu source hien tai.
- Lan dau hoac khi can bootstrap descriptions.yaml, chay scripts/generate-api-docs.ps1 -BootstrapDescriptions.

