using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.PaymentContext.Enums;

public sealed class TransactionStatus : EnumValueObject<TransactionStatus>
{
    public static readonly TransactionStatus Pending = new("pending");
    public static readonly TransactionStatus Processing = new("processing");
    public static readonly TransactionStatus Completed = new("completed");
    public static readonly TransactionStatus Failed = new("failed");
    public static readonly TransactionStatus Cancelled = new("cancelled");
    public static readonly TransactionStatus Refunded = new("refunded");
    private TransactionStatus(string id) : base(id) { }
}