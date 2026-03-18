using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.Enums;

public sealed class PerformerType : EnumValueObject<PerformerType>
{
    public static readonly PerformerType Seller = new("seller");
    public static readonly PerformerType Buyer = new("buyer");
    public static readonly PerformerType Admin = new("admin");
    public static readonly PerformerType System = new("system");
    private PerformerType(string id) : base(id) { }
}