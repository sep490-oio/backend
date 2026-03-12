using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.AuctionContext.Enums;

public sealed class AuctionType : EnumValueObject<AuctionType>
{
    public static readonly AuctionType Regular = new("regular");
    public static readonly AuctionType Sealed = new("sealed");
    private AuctionType(string id) : base(id) { }
}