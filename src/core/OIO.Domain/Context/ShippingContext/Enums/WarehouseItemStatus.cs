using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ShippingContext.Enums;

public sealed class WarehouseItemStatus : EnumValueObject<WarehouseItemStatus>
{
    public static readonly WarehouseItemStatus Pending = new("pending");
    public static readonly WarehouseItemStatus Received = new("received");
    public static readonly WarehouseItemStatus Inspected = new("inspected");
    public static readonly WarehouseItemStatus Stored = new("stored");
    public static readonly WarehouseItemStatus Reserved = new("reserved");
    public static readonly WarehouseItemStatus Dispatched = new("dispatched");
    private WarehouseItemStatus(string id) : base(id) { }
}