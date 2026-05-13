using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.PaymentContext.Enums;

public sealed class EscrowReleaseType : EnumValueObject<EscrowReleaseType>
{
    public static readonly EscrowReleaseType Full = new("full");
    public static readonly EscrowReleaseType Partial = new("partial");
    public static readonly EscrowReleaseType Refund = new("refund");
    public static readonly EscrowReleaseType Adjustment = new("adjustment");
    public static readonly EscrowReleaseType Forfeit = new("forfeit");
    private EscrowReleaseType(string id) : base(id) { }
}