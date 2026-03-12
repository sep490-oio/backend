using OIO.Domain.SeedWork.Entities;
using Vogen;

namespace OIO.Domain.Context.ShippingContext.ValueObjects.Ids;

[ValueObject<Guid>] 
public readonly partial struct WarehouseItemId : IEntityId;