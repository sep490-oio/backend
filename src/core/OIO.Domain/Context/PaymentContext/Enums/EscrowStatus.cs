using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.PaymentContext.Enums;

public sealed class EscrowStatus : EnumValueObject<EscrowStatus>
{
    public static readonly EscrowStatus Holding = new("holding");
    public static readonly EscrowStatus ReleasedToSeller = new("released_to_seller");
    public static readonly EscrowStatus RefundedToBuyer = new("refunded_to_buyer");
    public static readonly EscrowStatus Disputed = new("disputed");
    private EscrowStatus(string id) : base(id) { }
}