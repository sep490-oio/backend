using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.AuctionContext.Enums;

public sealed class ParticipantJoinStatus : EnumValueObject<ParticipantJoinStatus>
{
    public static readonly ParticipantJoinStatus Invited = new("invited");
    public static readonly ParticipantJoinStatus Requested = new("requested");
    public static readonly ParticipantJoinStatus Joined = new("joined");
    public static readonly ParticipantJoinStatus Withdrawn = new("withdrawn");
    public static readonly ParticipantJoinStatus Rejected = new("rejected");
    private ParticipantJoinStatus(string id) : base(id) { }
}