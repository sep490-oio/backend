using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.AuctionContext.Enums;

public sealed class ParticipantQualificationStatus : EnumValueObject<ParticipantQualificationStatus>
{
    public static readonly ParticipantQualificationStatus Pending = new("pending");
    public static readonly ParticipantQualificationStatus Qualified = new("qualified");
    public static readonly ParticipantQualificationStatus Rejected = new("rejected");
    public static readonly ParticipantQualificationStatus Expired = new("expired");
    public static readonly ParticipantQualificationStatus Waived = new("waived");
    private ParticipantQualificationStatus(string id) : base(id) { }
}