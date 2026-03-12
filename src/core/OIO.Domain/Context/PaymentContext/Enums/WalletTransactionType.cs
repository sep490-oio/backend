using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.PaymentContext.Enums;

public sealed class WalletTransactionType : EnumValueObject<WalletTransactionType>
{
    public static readonly WalletTransactionType Credit = new("credit");
    public static readonly WalletTransactionType Debit = new("debit");
    public static readonly WalletTransactionType Hold = new("hold");
    public static readonly WalletTransactionType Release = new("release");
    private WalletTransactionType(string id) : base(id) { }
}