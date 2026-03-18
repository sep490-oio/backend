using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.AuctionContext.Enums;

public sealed class SealedBidStatus : EnumValueObject<SealedBidStatus>
{
    public static readonly SealedBidStatus Submitted = new("submitted");
    public static readonly SealedBidStatus Revealed = new("revealed");
    public static readonly SealedBidStatus Invalidated = new("invalidated");
    public static readonly SealedBidStatus Withdrawn = new("withdrawn");
    private SealedBidStatus(string id) : base(id) { }
}