using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.PaymentContext.Enums;

public sealed class WalletType : EnumValueObject<WalletType>
{
    public static readonly WalletType Personal = new("personal");
    public static readonly WalletType Platform = new("platform");
    private WalletType(string id) : base(id) { }
}
