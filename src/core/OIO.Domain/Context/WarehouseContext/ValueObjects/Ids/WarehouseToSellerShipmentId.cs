using OIO.Domain.SeedWork.Entities;
using Vogen;

namespace OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;

[ValueObject<Guid>]
public readonly partial struct WarehouseToSellerShipmentId : IEntityId;
