using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.PaymentContext.Enums;

public sealed class TransactionType : EnumValueObject<TransactionType>
{
    public static readonly TransactionType Payment = new("payment");
    public static readonly TransactionType Refund = new("refund");
    public static readonly TransactionType Deposit = new("deposit");
    public static readonly TransactionType Withdrawal = new("withdrawal");
    public static readonly TransactionType Fee = new("fee");
    public static readonly TransactionType Payout = new("payout");
    private TransactionType(string id) : base(id) { }
}