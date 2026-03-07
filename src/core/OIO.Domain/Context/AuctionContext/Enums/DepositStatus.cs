using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.AuctionContext.Enums;

public sealed class DepositStatus : EnumValueObject<DepositStatus>
{
    public static readonly DepositStatus Held = new("held");
    public static readonly DepositStatus Returned = new("returned");
    public static readonly DepositStatus Forfeited = new("forfeited");
    public static readonly DepositStatus ConvertedToPayment = new("converted_to_payment");

    public DepositStatus(string value) : base(value) { }
}