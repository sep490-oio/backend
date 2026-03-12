using OIO.Domain.Context.ShippingContext.ValueObjects;
using OIO.Domain.Context.ShippingContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ShippingContext.Aggregates;

public sealed class WarehouseStorageLocation : AggregateRoot<WarehouseStorageLocationId>, ICreatedAtEntity
{
    public StorageLocationCode Location { get; private set; }     // e.g. A-01-03-02
    public bool IsOccupied { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private WarehouseStorageLocation() { }
}