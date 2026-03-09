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

    public bool CanTransitionTo(AuctionStatus target)
    {
        return (this, target) switch
        {
            _ when this == Draft && target == Pending => true,
            _ when this == Draft && target == Cancelled => true,
            _ when this == Pending && target == Active => true,
            _ when this == Pending && target == Cancelled => true,
            _ when this == Active && target == Ended => true,
            _ when this == Active && target == Cancelled => true,
            _ when this == Ended && target == Sold => true,
            _ when this == Ended && target == Failed => true,
            _ => false
        };
    }

    public bool AcceptsBids => this == Active;
}