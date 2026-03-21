# 17-05 -- My Orders

## Endpoint

| Property | Value |
|----------|-------|
| Route | `GET /api/me/orders` |
| Permission | (authenticated -- no specific permission) |
| Tag | `Me` |
| Response | `IReadOnlyList<OrderDto>` |

---

## Key Characteristics

- **No filter parameters** -- the endpoint returns all orders for the current user.
- **No pagination** -- returns an `IReadOnlyList<OrderDto>`, not a `PagedList`.
- **Both buyer AND seller orders** -- returns orders where the current user is either the `BuyerId` or the `SellerId`.

---

## OrderDto (22 fields)

| Field | Type | Description |
|-------|------|-------------|
| `id` | `Guid` | Order identifier |
| `orderNumber` | `string` | Human-readable order number |
| `auctionId` | `Guid` | Associated auction ID |
| `buyerId` | `Guid` | Buyer user ID |
| `sellerId` | `Guid` | Seller user ID |
| `status` | `string` | Current order status |
| `totalAmount` | `decimal` | Total order amount |
| `currency` | `string` | Currency code |
| `createdAt` | `DateTime` | Order creation timestamp |
| `paymentDueAt` | `DateTime?` | Payment deadline |
| `paidAt` | `DateTime?` | When payment was completed |
| `shippedAt` | `DateTime?` | When the item was shipped |
| `deliveredAt` | `DateTime?` | When the item was delivered |
| `decisionWindowEndsAt` | `DateTime?` | Deadline for buyer to accept/dispute |
| `completedAt` | `DateTime?` | When the order was finalized |
| `cancelledAt` | `DateTime?` | When the order was cancelled |
| `escrowStatus` | `string?` | Escrow payment status |
| `trackingNumber` | `string?` | Shipping tracking number |
| `return` | `OrderReturnDto?` | Nested return request details (null if no return) |

### OrderReturnDto (12 fields)

| Field | Type | Description |
|-------|------|-------------|
| `id` | `Guid` | Return request ID |
| `status` | `string` | Return status |
| `reasonCode` | `string` | Return reason code |
| `description` | `string?` | User-provided reason text |
| `decisionReason` | `string?` | Admin/seller decision reason |
| `providerCode` | `string?` | Shipping provider code |
| `trackingNumber` | `string?` | Return shipment tracking number |
| `requestedAt` | `DateTime` | When the return was requested |
| `approvedAt` | `DateTime?` | When the return was approved |
| `rejectedAt` | `DateTime?` | When the return was rejected |
| `shippedAt` | `DateTime?` | When the return item was shipped |
| `sellerReceivedAt` | `DateTime?` | When seller received the returned item |
| `buyerDecisionDueAt` | `DateTime?` | Deadline for buyer decision on return |

---

## Source References

| File | Path |
|------|------|
| Endpoint | `src/presentation/OIO.Api/Endpoints/UserContext/Me/GetMyOrdersEndpoint.cs` |
| Query | `src/core/OIO.Application/Context/OrderContext/Queries/GetMyOrders/GetMyOrdersQuery.cs` |
| DTO | `src/core/OIO.Application/Context/OrderContext/DTOs/OrderDto.cs` |
| Return DTO | `src/core/OIO.Application/Context/OrderContext/DTOs/OrderReturnDto.cs` |
