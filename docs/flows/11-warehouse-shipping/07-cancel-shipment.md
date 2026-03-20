# Cancel Shipment

Cancels inbound or outbound shipments, calling the GHN cancel API if a carrier booking exists.

## Cancel Inbound Shipment

### Endpoint

```
POST /api/warehouse/inbound-shipments/{shipmentId}/cancel
Permission: Warehouse.ReadShipments
```

### Request Body

```json
{
  "Reason": "string"
}
```

### Handler Logic (CancelInboundShipmentCommandHandler)

1. **Load shipment** by `InboundShipmentId`
2. **Domain validation**: `shipment.Cancel(reason, now)`
   - Cannot cancel if status is `Completed`, `Cancelled`, or `Failed`
   - Sets `Status = Cancelled`, raises `InboundShipmentCancelledEvent`
3. **Cancel with GHN** (if `CarrierTrackingNumber` is not null):
   - Load active `ShippingProviderConfig` matching `shipment.ProviderCode`
   - Call `IShippingService.CancelShipmentAsync(providerCode, carrierTrackingNumber, config)`
   - If GHN cancel fails, log warning and **return the error** (cancel is aborted)
4. **SaveChanges**

### Responses

| Status | Condition |
|---|---|
| 204 No Content | Successfully cancelled |
| 404 Not Found | Shipment not found |
| 409 Conflict | Status is `Completed`, `Cancelled`, or `Failed` |

## Cancel Outbound Shipment

### Endpoint

```
POST /api/warehouse/outbound-shipments/{shipmentId}/cancel
Permission: Warehouse.ReadShipments
```

### Request Body

```json
{
  "Reason": "string"
}
```

### Handler Logic (CancelOutboundShipmentCommandHandler)

1. **Load shipment** by `OutboundShipmentId`
2. **Domain validation**: `shipment.Cancel(reason, now)`
   - Cannot cancel if status is `PickedUp`, `InTransit`, or `Delivered`
   - Sets `Status = Cancelled`, raises `OutboundShipmentCancelledEvent`
3. **Cancel with GHN** (if `CarrierTrackingNumber` is not null):
   - Load active `ShippingProviderConfig` matching `shipment.ProviderCode`
   - Call `IShippingService.CancelShipmentAsync(providerCode, carrierTrackingNumber, config)`
   - If GHN cancel fails, log warning and **return the error** (cancel is aborted)
4. **Restore WarehouseItem**:
   - Load `WarehouseItem` by `shipment.WarehouseItemId`
   - If item exists and still has a `StorageLocationId`, call `warehouseItem.Store(locationId, "", now)` to reset status back to `Stored`
5. **SaveChanges**

### Responses

| Status | Condition |
|---|---|
| 204 No Content | Successfully cancelled |
| 404 Not Found | Shipment not found |
| 409 Conflict | Status is `PickedUp`, `InTransit`, or `Delivered` |

## GHN Cancel API

```
POST /shiip/public-api/v2/switch-status/cancel
Headers: Token, ShopId
```

### Request

```json
{
  "order_codes": ["TRACKING_NUMBER"]
}
```

### Response

```json
{
  "code": 200,
  "message": "Success",
  "data": [
    {
      "order_code": "TRACKING_NUMBER",
      "result": true,
      "message": null
    }
  ]
}
```

The handler checks the per-order `result` field. If `result: false`, the cancellation is rejected by GHN and the error is returned with code `Ghn.CancelOrder.Rejected`.

## Cancellation Rules Summary

### Inbound Shipment

| Current Status | Can Cancel? |
|---|---|
| `AwaitingPickup` | Yes |
| `InTransit` | Yes |
| `Arrived` | Yes |
| `Inspected` | Yes |
| `Completed` | No -- `InboundShipment.CannotCancel` |
| `Cancelled` | No -- `InboundShipment.CannotCancel` |
| `Failed` | No -- `InboundShipment.CannotCancel` |

### Outbound Shipment

| Current Status | Can Cancel? |
|---|---|
| `Pending` | Yes |
| `Booked` | Yes |
| `PickedUp` | No -- `OutboundShipment.CannotCancel` |
| `InTransit` | No -- `OutboundShipment.CannotCancel` |
| `Delivered` | No -- `OutboundShipment.CannotCancel` |
| `Failed` | Yes |
| `Returning` | Yes |
| `Returned` | Yes |
| `Cancelled` | Yes (idempotent -- already cancelled) |

## Error Codes

| Code | HTTP | Condition |
|---|---|---|
| `InboundShipment.NotFound` | 404 | Inbound shipment does not exist |
| `InboundShipment.CannotCancel` | 409 | Status is `Completed`, `Cancelled`, or `Failed` |
| `OutboundShipment.NotFound` | 404 | Outbound shipment does not exist |
| `OutboundShipment.CannotCancel` | 409 | Status is `PickedUp`, `InTransit`, or `Delivered` |
| `Ghn.CancelOrder.HttpError` | 500 | GHN returned non-success HTTP |
| `Ghn.CancelOrder.ApiError` | 500 | GHN returned code != 200 |
| `Ghn.CancelOrder.Rejected` | 500 | GHN per-order `result: false` |
| `Ghn.CancelOrder.Exception` | 500 | Unhandled exception calling GHN |
| `Ghn.Credentials.Invalid` | 500 | Missing token or shop_id in config |
| `Ghn.Credentials.ParseError` | 500 | Failed to deserialize credentials JSON |
