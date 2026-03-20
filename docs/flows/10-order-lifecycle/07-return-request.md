# Return Request

## Overview

After delivery, the buyer can request a return during the decision window. This creates an `OrderReturn` entity in `Requested` status and notifies the seller.

## Endpoint

| Property | Value |
|---|---|
| Method | `POST` |
| URL | `/api/orders/{orderId}/returns` |
| Auth | Required (buyer only) |
| Success | `200 OK` with `OrderReturnDto` |
| Tags | Orders |

**Source:** `src/presentation/OIO.Api/Endpoints/OrderContext/CreateOrderReturnEndpoint.cs`

## Request Body

```json
{
  "reasonCode": "string (required)",
  "description": "string (optional)"
}
```

The endpoint maps to `RequestOrderReturnCommand(OrderId, ReasonCode, Description?)`.

## Command Validation (`IHasValidate`)

| Field | Rule |
|---|---|
| `OrderId` | `NotEmptyGuid()` |
| `ReasonCode` | `NotWhiteSpace()` |

## Handler Logic

**Source:** `src/core/OIO.Application/Context/OrderContext/Commands/RequestOrderReturn/RequestOrderReturnCommand.cs`

1. Load the order by ID (includes `Return` navigation)
2. Call `order.RequestReturn(currentUser.UserId, reasonCode, description, DateTime.UtcNow)`
3. Insert the created `OrderReturn` entity
4. `SaveChangesAsync()`
5. Dispatch notification to seller

## Domain Validation: `Order.RequestReturn()`

**Source:** `src/core/OIO.Domain/Context/OrderContext/Aggregates/Orders/Order.cs`

The domain method enforces the following rules in order:

| # | Check | Error Code | HTTP Status |
|---|---|---|---|
| 1 | `BuyerId != buyerId` | `Order.ReturnForbidden` | 403 Forbidden |
| 2 | `Status != OrderStatus.Delivered` | `Order.InvalidState` | 409 Conflict |
| 3 | `DecisionWindowEndsAt is null` | `Order.DecisionWindowNotStarted` | 409 Conflict |
| 4 | `nowUtc > DecisionWindowEndsAt.Value` | `Order.DecisionWindowExpired` | 409 Conflict |
| 5 | Return exists with status not Cancelled/Resolved/Rejected | `Order.ReturnAlreadyExists` | 409 Conflict |

If all checks pass, an `OrderReturn` is created and assigned to `order.Return`.

## Entity Creation: `OrderReturn.Create()`

**Source:** `src/core/OIO.Domain/Context/OrderContext/Aggregates/Orders/OrderReturn.cs`

```
OrderReturn.Create(orderId, buyerId, reasonCode, description, buyerDecisionDueAt, nowUtc)
```

| Property | Value |
|---|---|
| `Id` | `OrderReturnId.From(Guid.CreateVersion7())` |
| `OrderId` | From the parent order |
| `BuyerId` | Current user's ID |
| `ReasonCode` | From request |
| `Description` | From request (nullable) |
| `Status` | `OrderReturnStatus.Requested` |
| `RequestedAt` | `nowUtc` |
| `BuyerDecisionDueAt` | Set to `order.DecisionWindowEndsAt.Value` |

## Notification

After successful creation, a notification is dispatched to the **seller**:

| Property | Value |
|---|---|
| `UserId` | `order.SellerId` |
| `NotificationType` | `"order"` |
| `EventType` | `"return_requested"` |
| `Priority` | `High` |
| `EntityType` | `"Order"` |
| `EntityId` | `order.Id` |
| `Metadata` | `{ orderId, returnId, reasonCode }` |

## Error Codes Summary

| Code | Description | HTTP Status |
|---|---|---|
| `Order.NotFound` | Order does not exist | 404 |
| `Order.ReturnForbidden` | Caller is not the buyer | 403 |
| `Order.InvalidState` | Order is not in Delivered status | 409 |
| `Order.DecisionWindowNotStarted` | `DecisionWindowEndsAt` is null | 409 |
| `Order.DecisionWindowExpired` | Current time past decision window | 409 |
| `Order.ReturnAlreadyExists` | An active (non-terminal) return already exists | 409 |

## Produced HTTP Responses

| Status | Description |
|---|---|
| `200 OK` | Return request created, returns `OrderReturnDto` |
| `400 Bad Request` | Validation problem (missing fields) |
| `401 Unauthorized` | Not authenticated |
| `403 Forbidden` | Not the buyer |
| `404 Not Found` | Order not found |
| `409 Conflict` | Invalid state / window expired / return exists |
