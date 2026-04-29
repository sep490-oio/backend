using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.AuctionContext.Enums;

public sealed class WinnerOfferStatus : EnumValueObject<WinnerOfferStatus>
{
    public static readonly WinnerOfferStatus Pending = new("pending");
    public static readonly WinnerOfferStatus Accepted = new("accepted");
    public static readonly WinnerOfferStatus Declined = new("declined");
    public static readonly WinnerOfferStatus Expired = new("expired");
    public static readonly WinnerOfferStatus Defaulted = new("defaulted");
    public static readonly WinnerOfferStatus Cancelled = new("cancelled");
    private WinnerOfferStatus(string id) : base(id) { }
}
