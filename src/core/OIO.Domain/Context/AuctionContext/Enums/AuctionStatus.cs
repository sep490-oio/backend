using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.AuctionContext.Enums;

public sealed class AuctionStatus : EnumValueObject<AuctionStatus>
{
    public static readonly AuctionStatus Draft = new("draft");
    public static readonly AuctionStatus Pending = new("pending");
    public static readonly AuctionStatus Approved = new("approved");
    public static readonly AuctionStatus Scheduled = new("scheduled");
    public static readonly AuctionStatus Active = new("active");
    public static readonly AuctionStatus Ended = new("ended");
    public static readonly AuctionStatus Sold = new("sold");
    public static readonly AuctionStatus PaymentDefaulted = new("payment_defaulted");
    public static readonly AuctionStatus Cancelled = new("cancelled");
    public static readonly AuctionStatus Failed = new("failed");
    public static readonly AuctionStatus Terminated = new("terminated");

    public AuctionStatus(string id) : base(id) { }

    public bool AcceptsBids => this == Active;

    public bool CanTransitionTo(AuctionStatus target) =>
        (Id, target.Id) switch
        {
            ("draft", "pending") => true,
            ("draft", "cancelled") => true,
            ("pending", "approved") => true,
            ("pending", "cancelled") => true,
            ("approved", "scheduled") => true,
            ("approved", "cancelled") => true,
            ("scheduled", "active") => true,
            ("scheduled", "cancelled") => true,
            ("scheduled", "sold") => true,
            ("scheduled", "terminated") => true,
            ("active", "ended") => true,
            ("active", "cancelled") => true,
            ("active", "terminated") => true,
            ("active", "sold") => true,
            ("ended", "sold") => true,
            ("ended", "failed") => true,
            ("sold", "payment_defaulted") => true,
            ("payment_defaulted", "sold") => true,
            ("payment_defaulted", "scheduled") => true,
            ("sold", "terminated") => true,
            ("payment_defaulted", "terminated") => true,
            ("ended", "terminated") => true,
            _ => false
        };
}
