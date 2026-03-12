using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.AuctionContext.Enums;

public sealed class AuctionStatus : EnumValueObject<AuctionStatus>
{
    public static readonly AuctionStatus Draft = new("draft");
    public static readonly AuctionStatus Pending = new("pending");
    public static readonly AuctionStatus Active = new("active");
    public static readonly AuctionStatus Ended = new("ended");
    public static readonly AuctionStatus Sold = new("sold");
    public static readonly AuctionStatus Cancelled = new("cancelled");
    public static readonly AuctionStatus Failed = new("failed");

    public AuctionStatus(string id) : base(id) { }

    public bool AcceptsBids => this == Active;

    public bool CanTransitionTo(AuctionStatus target) =>
        (Id, target.Id) switch
        {
            ("draft", "pending") => true,
            ("pending", "active") => true,
            ("pending", "cancelled") => true,
            ("active", "ended") => true,
            ("active", "cancelled") => true,
            ("active", "sold") => true,
            ("ended", "sold") => true,
            ("ended", "failed") => true,
            _ => false
        };
}