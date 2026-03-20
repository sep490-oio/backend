# 04 -- Store Warehouse Item

> After inspection review is approved, warehouse staff assigns the item to a physical storage location.

---

## Endpoint

| Field | Value |
|---|---|
| **Method** | `POST` |
| **URL** | `/api/warehouse/warehouse-items/{warehouseItemId}/store` |
| **Permission** | `warehouse:item:store` (`Catalogs.Warehouse.Store`) |
| **Response** | `200 OK` with `WarehouseItemDto` |

### Request DTO: `StoreWarehouseItemCommand`

| Field | Type | Required | Description |
|---|---|---|---|
| `WarehouseItemId` | `Guid` | Yes | Path param: the warehouse item to store |
| `StorageLocationId` | `Guid` | Yes | Target storage location |

---

## Handler Logic (`StoreWarehouseItemCommandHandler`)

1. **Load WarehouseItem** by ID. Error: `WarehouseItem.NotFound`.

2. **Load StorageLocation** by ID. Error: `StorageLocation.NotFound`.

3. **Check location not occupied** -- `location.IsOccupied` must be `false`. Error: `StorageLocation.Occupied`.

4. **Store item** -- `warehouseItem.Store(locationId, location.Label, now)`:
   - Sets `StorageLocationId` on the warehouse item
   - Changes status from `Inspected` -> `Stored`
   - Raises `WarehouseItemStoredEvent(warehouseItemId, locationId, locationLabel)`
   - Error if not in `Inspected` status: `WarehouseItem.NotInspected`
   - Error if already has a location: `WarehouseItem.AlreadyStored`

5. **Mark location occupied** -- `location.MarkOccupied()` sets `IsOccupied = true`.

6. **Complete InboundShipment** -- loads the parent `InboundShipment` by `warehouseItem.InboundShipmentId`:
   - `shipment.Complete(now)` changes status from `Inspected` -> `Completed`
   - Raises `InboundShipmentCompletedEvent`
   - Error if not in `Inspected` status: `InboundShipment.CannotComplete`

7. **Persist** -- `SaveChangesAsync()`.

---

## State Transitions

```
WarehouseItem:     Inspected  -->  Stored
StorageLocation:   IsOccupied = false  -->  IsOccupied = true
InboundShipment:   Inspected  -->  Completed
```

---

## Storage Location Label Format

Labels follow the pattern: **`ZONE-AISLE-SHELF-BIN`**

Examples:
- `A-01-03-02` -- Zone A, Aisle 01, Shelf 03, Bin 02
- `B-02-01-05` -- Zone B, Aisle 02, Shelf 01, Bin 05

Label is auto-generated from the four components: `$"{Zone}-{Aisle}-{Shelf}-{Bin}"`.

Zone is normalized to uppercase. All components are trimmed.

---

## Storage Location Management

Storage locations are managed via CRUD endpoints:

| Method | URL | Permission | Description |
|---|---|---|---|
| `GET` | `/api/warehouse/storage-locations` | `warehouse:locations:manage` | List all locations |
| `POST` | `/api/warehouse/storage-locations` | `warehouse:locations:manage` | Create location `{ Zone, Aisle, Shelf, Bin }` |
| `PUT` | `/api/warehouse/storage-locations/{locationId}` | `warehouse:locations:manage` | Update location components |
| `DELETE` | `/api/warehouse/storage-locations/{locationId}` | `warehouse:locations:manage` | Delete location |

Create validates that the computed label is unique (duplicate label guard: `StorageLocation.LabelAlreadyExists`).

**Source:** `WarehouseStorageLocation.cs` -- `Create()`, `Update()`, `MarkOccupied()`, `MarkVacant()`.

---

## Error Codes

| Code | When |
|---|---|
| `WarehouseItem.NotFound` | Warehouse item ID does not exist |
| `StorageLocation.NotFound` | Storage location ID does not exist |
| `StorageLocation.Occupied` | Target location is already occupied by another item |
| `WarehouseItem.NotInspected` | Item is not in `Inspected` status (cannot store) |
| `WarehouseItem.AlreadyStored` | Item already has a storage location assigned |
| `InboundShipment.CannotComplete` | Shipment not in `Inspected` status |
| `StorageLocation.LabelAlreadyExists` | Duplicate label on create |
