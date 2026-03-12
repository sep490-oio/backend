using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ShippingContext.Enums;

public sealed class WarehouseItemCondition : EnumValueObject<WarehouseItemCondition>
{
    public static readonly WarehouseItemCondition New = new("new");
    public static readonly WarehouseItemCondition LikeNew = new("like_new");
    public static readonly WarehouseItemCondition VeryGood = new("very_good");
    public static readonly WarehouseItemCondition Good = new("good");
    public static readonly WarehouseItemCondition Acceptable = new("acceptable");
    public static readonly WarehouseItemCondition Damaged = new("damaged");
    private WarehouseItemCondition(string id) : base(id) { }
}