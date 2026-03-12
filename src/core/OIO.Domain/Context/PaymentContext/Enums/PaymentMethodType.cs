using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.PaymentContext.Enums;

public sealed class PaymentMethodType : EnumValueObject<PaymentMethodType>
{
    public static readonly PaymentMethodType CreditCard = new("credit_card");
    public static readonly PaymentMethodType DebitCard = new("debit_card");
    public static readonly PaymentMethodType BankAccount = new("bank_account");
    public static readonly PaymentMethodType EWallet = new("e_wallet");
    private PaymentMethodType(string id) : base(id) { }
}