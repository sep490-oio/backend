# Warehouse Flow

## Inbound Shipment Status State Machine

Derived from `InboundShipment.cs` lifecycle methods.

```mermaid
stateDiagram-v2
    [*] --> AwaitingPickup: create (seller books shipment)

    AwaitingPickup --> InTransit: carrier picks up (webhook: picked_up/in_transit)
    AwaitingPickup --> Cancelled: cancel
    AwaitingPickup --> Failed: carrier fails (webhook: failed/returning/returned)

    InTransit --> Arrived: carrier delivers to warehouse (webhook: delivered)
    InTransit --> Cancelled: cancel
    InTransit --> Failed: carrier fails

    Arrived --> Inspected: warehouse staff inspects (RecordInspected)
    Arrived --> Cancelled: cancel
    Arrived --> Failed: fail

    Inspected --> Completed: WarehouseItem created + stored
    Inspected --> Cancelled: cancel
    Inspected --> Failed: fail

    Completed --> [*]
    Cancelled --> [*]
    Failed --> [*]
```

## Warehouse Item Status State Machine

Derived from `WarehouseItem.cs` transition methods.

```mermaid
stateDiagram-v2
    [*] --> Pending: create (from InboundShipment)

    Pending --> Received: MarkReceived (physically received)

    Received --> Inspected: MarkInspected (inspection complete)

    Inspected --> Stored: Store(locationId) - placed on shelf

    Stored --> Reserved: Reserve(outboundShipmentId) - order paid

    Reserved --> Dispatched: MarkDispatched - carrier picks up

    Dispatched --> [*]
```

## Warehouse Inspection Decision Status

```mermaid
stateDiagram-v2
    [*] --> PendingReview: inspection created

    PendingReview --> Approved: reviewer approves (condition matches)
    PendingReview --> Rejected: reviewer rejects
    PendingReview --> ConditionConfirmationRequired: condition differs

    ConditionConfirmationRequired --> ConditionConfirmed: seller confirms updated condition

    Approved --> [*]
    Rejected --> [*]
    ConditionConfirmed --> [*]
```

## Outbound Shipment Status State Machine

```mermaid
stateDiagram-v2
    [*] --> Pending: create (order paid)

    Pending --> Booked: carrier order created

    Booked --> PickedUp: carrier picks up from warehouse

    PickedUp --> InTransit: carrier in transit

    InTransit --> Delivered: carrier delivers to buyer
    InTransit --> Failed: delivery failed
    InTransit --> Returning: return initiated

    Delivered --> [*]

    Failed --> Returning: carrier returning
    Returning --> Returned: returned to warehouse

    Pending --> Cancelled: cancel
    Booked --> Cancelled: cancel

    Returned --> [*]
    Cancelled --> [*]
    Failed --> [*]
```

## Full Warehouse Sequence (Inbound to Outbound)

```mermaid
sequenceDiagram
    autonumber
    participant Seller
    participant API
    participant GHN as GHN Carrier
    participant Warehouse as Warehouse Staff
    participant DB
    participant Buyer

    rect rgb(230, 245, 255)
        Note over Seller,DB: Phase 1 - Inbound Shipment
        Seller->>API: Request inbound shipment for item
        API->>GHN: Create shipping order (sender=seller, receiver=warehouse)
        GHN-->>API: {carrierTrackingNumber}
        API->>DB: InboundShipment(status=AwaitingPickup)
        API->>DB: RecordBooked(carrierTrackingNumber)

        GHN->>API: Webhook: picked_up
        API->>DB: InboundShipment.Status = InTransit
        API->>DB: Record ShipmentTrackingEvent

        GHN->>API: Webhook: delivered
        API->>DB: InboundShipment.Status = Arrived
    end

    rect rgb(255, 245, 230)
        Note over Warehouse,DB: Phase 2 - Inspection
        Warehouse->>API: Record inspection (condition, evidence photos)
        API->>DB: InboundShipment.RecordInspected()
        API->>DB: WarehouseItem.Create()
        API->>DB: WarehouseItem.MarkReceived()
        API->>DB: WarehouseInspection.Create(declaredCondition, conditionOnArrival, evidence)

        alt Condition matches
            Warehouse->>API: Approve inspection
            API->>DB: WarehouseInspection.Approve()
            API->>DB: WarehouseItem.MarkInspected()
            API->>DB: Item.Status = Approved
        else Condition differs
            Warehouse->>API: Require condition confirmation
            API->>DB: WarehouseInspection.RequireConditionConfirmation()
            API->>DB: Item.Status = PendingConditionConfirmation
            Seller->>API: Confirm updated condition
            API->>DB: WarehouseInspection.ConfirmSellerCondition()
            API->>DB: Item.Status = Approved
        end
    end

    rect rgb(230, 255, 230)
        Note over Warehouse,DB: Phase 3 - Storage
        Warehouse->>API: Assign storage location
        API->>DB: WarehouseItem.Store(locationId)
        API->>DB: InboundShipment.Complete()
    end

    rect rgb(245, 230, 255)
        Note over API,Buyer: Phase 4 - Outbound (after auction sold + payment)
        API->>DB: OutboundShipment.Create(orderId)
        API->>DB: WarehouseItem.Reserve(outboundShipmentId)
        API->>GHN: Create shipping order (sender=warehouse, receiver=buyer)
        GHN-->>API: {carrierTrackingNumber}
        API->>DB: OutboundShipment.Status = Booked

        GHN->>API: Webhook: picked_up
        API->>DB: OutboundShipment.Status = PickedUp
        API->>DB: WarehouseItem.MarkDispatched()

        GHN->>API: Webhook: in_transit
        API->>DB: OutboundShipment.Status = InTransit

        GHN->>API: Webhook: delivered
        API->>DB: OutboundShipment.Status = Delivered
        API->>DB: Order.MarkAsDelivered()
        API->>Buyer: Notification "Your item has been delivered"
    end
```
