# Order Queries

## Overview

Two read endpoints provide order data: one for fetching a specific order by ID, and one for listing all orders belonging to the current user. Both return the same `OrderDto` structure.

---

## GET /api/orders/{orderId} -- Get Order by ID

### Endpoint

| Property | Value |
|---|---|
| Method | `GET` |
| URL | `/api/orders/{orderId}` |
| Auth | Required (buyer or seller of the order) |
| Success | `200 OK` with `OrderDto` |

**Source:** `src/presentation/OIO.Api/Endpoints/OrderContext/GetOrderByIdEndpoint.cs`

### Handler Logic

**Source:** `src/core/OIO.Application/Context/OrderContext/Queries/GetOrderById/GetOrderByIdQuery.cs`

1. Query `Order` with `AsNoTracking()`, includes: `Return`, `Escrows`, `OutboundShipments`
2. If not found: return `404` with code `Order.NotFound`
3. Authorization check: `order.BuyerId == currentUser.UserId` OR `order.SellerId == currentUser.UserId`
   - If neither: return `403` with code `Order.Forbidden`
4. Map to `OrderDto` via `order.ToDto()`

### Produced HTTP Responses

| Status | Description |
|---|---|
| `200 OK` | Returns `OrderDto` |
| `401 Unauthorized` | Not authenticated |
| `403 Forbidden` | Caller is neither buyer nor seller |
| `404 Not Found` | Order does not exist |

---

## GET /api/me/orders -- Get My Orders

### Endpoint

| Property | Value |
|---|---|
| Method | `GET` |
| URL | `/api/me/orders` |
| Auth | Required |
| Success | `200 OK` with `IReadOnlyList<OrderDto>` |
| Tags | Me |

**Source:** `src/presentation/OIO.Api/Endpoints/UserContext/Me/GetMyOrdersEndpoint.cs`

### Handler Logic

**Source:** `src/core/OIO.Application/Context/OrderContext/Queries/GetMyOrders/GetMyOrdersQuery.cs`

1. Query `Order` with `AsNoTracking()`, includes: `Return`, `Escrows`, `OutboundShipments`
2. Filter: `BuyerId == currentUser.UserId` OR `SellerId == currentUser.UserId`
3. Order by `CreatedAt` descending (newest first)
4. Map all results to `OrderDto` list

Note: The current implementation returns all matching orders without pagination. The query record `GetMyOrdersQuery` takes no parameters.

### Produced HTTP Responses

| Status | Description |
|---|---|
| `200 OK` | Returns list of `OrderDto` |
| `401 Unauthorized` | Not authenticated |

---

## OrderDto Structure

**Source:** `src/core/OIO.Application/Context/OrderContext/DTOs/OrderDto.cs`

```csharp
public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    Guid AuctionId,
    Guid BuyerId,
    Guid SellerId,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt,
    DateTime? PaymentDueAt,
    DateTime? PaidAt,
    DateTime? ShippedAt,
    DateTime? DeliveredAt,
    DateTime? DecisionWindowEndsAt,
    DateTime? CompletedAt,
    DateTime? CancelledAt,
    string? EscrowStatus,
    string? TrackingNumber,
    OrderReturnDto? Return);
```

| Field | Type | Description |
|---|---|---|
| `Id` | `Guid` | Order ID |
| `OrderNumber` | `string` | Human-readable order number |
| `AuctionId` | `Guid` | Associated auction |
| `BuyerId` | `Guid` | Buyer user ID |
| `SellerId` | `Guid` | Seller user ID |
| `Status` | `string` | Current order status |
| `TotalAmount` | `decimal` | Order total |
| `Currency` | `string` | Currency code |
| `CreatedAt` | `DateTime` | Order creation timestamp |
| `PaymentDueAt` | `DateTime?` | Payment deadline |
| `PaidAt` | `DateTime?` | When payment was made |
| `ShippedAt` | `DateTime?` | When order was shipped |
| `DeliveredAt` | `DateTime?` | When order was delivered |
| `DecisionWindowEndsAt` | `DateTime?` | Decision window expiry |
| `CompletedAt` | `DateTime?` | When order was completed |
| `CancelledAt` | `DateTime?` | When order was cancelled |
| `EscrowStatus` | `string?` | Current escrow status |
| `TrackingNumber` | `string?` | Shipping tracking number |
| `Return` | `OrderReturnDto?` | Return details if a return exists |

---

## OrderReturnDto Structure

**Source:** `src/core/OIO.Application/Context/OrderContext/DTOs/OrderReturnDto.cs`

```csharp
public sealed record OrderReturnDto(
    Guid Id,
    string Status,
    string ReasonCode,
    string? Description,
    string? DecisionReason,
    string? ProviderCode,
    string? TrackingNumber,
    DateTime RequestedAt,
    DateTime? ApprovedAt,
    DateTime? RejectedAt,
    DateTime? ShippedAt,
    DateTime? SellerReceivedAt,
    DateTime? BuyerDecisionDueAt);
```

| Field | Type | Description |
|---|---|---|
| `Id` | `Guid` | Return request ID |
| `Status` | `string` | Current return status |
| `ReasonCode` | `string` | Buyer's reason code |
| `Description` | `string?` | Buyer's description |
| `DecisionReason` | `string?` | Seller's approval/rejection notes |
| `ProviderCode` | `string?` | Shipping provider code |
| `TrackingNumber` | `string?` | Return shipment tracking number |
| `RequestedAt` | `DateTime` | When return was requested |
| `ApprovedAt` | `DateTime?` | When return was approved |
| `RejectedAt` | `DateTime?` | When return was rejected |
| `ShippedAt` | `DateTime?` | When buyer shipped the return |
| `SellerReceivedAt` | `DateTime?` | When seller confirmed receipt |
| `BuyerDecisionDueAt` | `DateTime?` | Decision window deadline |

---

## Authorization Summary

| Endpoint | Who Can Access |
|---|---|
| `GET /api/orders/{orderId}` | Buyer or seller of that specific order |
| `GET /api/me/orders` | Any authenticated user (sees own orders as buyer or seller) |
