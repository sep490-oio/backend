using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ModerationContext.Enums;

public sealed class RefundType : EnumValueObject<RefundType>
{
    public static readonly RefundType Full = new("full");
    public static readonly RefundType Partial = new("partial");
    public static readonly RefundType ShippingOnly = new("shipping_only");
    public static readonly RefundType Compensation = new("compensation");
    private RefundType(string id) : base(id) { }
}