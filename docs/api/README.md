# API Reference

Tai lieu nay la reference chinh cho public surface cua OIO, bao gom HTTP API, callback/webhook va SignalR.

## Quy uoc chung

- Auth: mac dinh la Authorization: Bearer <token> neu route khong AllowAnonymous.
- Error format: route dung ProblemDetails / ValidationProblemDetails cho loi validation, auth, permission, state transition.
- Paging: cac endpoint list dung PagedList<T> se tra items va metadata.
- Timestamp: uu tien DateTime / DateTimeOffset nhu trong source.
- Currency: giu nguyen field currency theo payload runtime.
- Idempotency: cac route co IdempotencyFilter yeu cau header Idempotency-Key.
- Webhook/callback: VNPay va GHN la provider-driven route; contract co the khac flow UI thong thuong.

## Muc luc

- [User + Auth](./user.md)
- [Auction + Catalog](./auction.md)
- [Payment + Order](./payment-order.md)
- [Moderation + Warehouse](./moderation-warehouse.md)
- [SignalR](./signalr.md)
- [Schemas appendix](./schemas.md)

## Tooling

- Generator: scripts/generate-api-docs.ps1
- Generator se fail neu co endpoint hoac hub method moi chua co purpose trong descriptions.yaml.

