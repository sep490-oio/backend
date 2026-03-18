using OIO.Domain.SeedWork.Entities;
using Vogen;

namespace OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;

[ValueObject<Guid>]
public readonly partial struct InboundShipmentId : IEntityId;

[ValueObject<Guid>]
public readonly partial struct WarehouseItemId : IEntityId;

[ValueObject<Guid>]
public readonly partial struct WarehouseInspectionId : IEntityId;

[ValueObject<Guid>]
public readonly partial struct OutboundShipmentId : IEntityId;

[ValueObject<Guid>]
public readonly partial struct ShipmentTrackingEventId : IEntityId;

[ValueObject<Guid>]
public readonly partial struct ShippingProviderConfigId : IEntityId;

[ValueObject<Guid>]
public readonly partial struct WarehouseStorageLocationId : IEntityId;
