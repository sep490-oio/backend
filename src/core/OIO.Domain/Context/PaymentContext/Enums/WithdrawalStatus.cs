using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.PaymentContext.Enums;

public sealed class WithdrawalStatus : EnumValueObject<WithdrawalStatus>
{
    public static readonly WithdrawalStatus Pending = new("pending");
    public static readonly WithdrawalStatus Approved = new("approved");
    public static readonly WithdrawalStatus Processing = new("processing");
    public static readonly WithdrawalStatus Completed = new("completed");
    public static readonly WithdrawalStatus Rejected = new("rejected");
    public static readonly WithdrawalStatus Cancelled = new("cancelled");
    private WithdrawalStatus(string id) : base(id) { }
}