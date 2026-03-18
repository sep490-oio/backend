using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class AuctionParticipant : BaseEntity<AuctionParticipantId>
{
    public AuctionId AuctionId { get; private set; }
    public UserId UserId { get; private set; }
    public string RoleInAuction { get; private set; }
    public ParticipantJoinStatus JoinStatus { get; private set; }
    public ParticipantQualificationStatus QualificationStatus { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public DateTime? QualifiedAt { get; private set; }
    public string? RejectedReason { get; private set; }

    // Navigation
    public Auction Auction { get; private set; } = null!;

    private AuctionParticipant() { }

    private AuctionParticipant(
        AuctionParticipantId id,
        AuctionId auctionId,
        UserId userId,
        string roleInAuction,
        ParticipantJoinStatus joinStatus,
        ParticipantQualificationStatus qualificationStatus,
        DateTime joinedAt)
        : base(id)
    {
        AuctionId = auctionId;
        UserId = userId;
        RoleInAuction = roleInAuction;
        JoinStatus = joinStatus;
        QualificationStatus = qualificationStatus;
        JoinedAt = joinedAt;
    }

    public static AuctionParticipant Create(
        AuctionId auctionId,
        UserId userId,
        string roleInAuction,
        DateTime nowUtc)
    {
        return new AuctionParticipant(
            AuctionParticipantId.From(Guid.CreateVersion7()),
            auctionId,
            userId,
            roleInAuction,
            ParticipantJoinStatus.Joined,
            ParticipantQualificationStatus.Qualified,
            nowUtc)
        {
            QualifiedAt = nowUtc
        };
    }

    public bool IsQualified =>
        JoinStatus == ParticipantJoinStatus.Joined &&
        QualificationStatus == ParticipantQualificationStatus.Qualified;

    public void Qualify(DateTime nowUtc)
    {
        JoinStatus = ParticipantJoinStatus.Joined;
        QualificationStatus = ParticipantQualificationStatus.Qualified;
        QualifiedAt = nowUtc;
        RejectedReason = null;
    }

    public void Withdraw()
    {
        JoinStatus = ParticipantJoinStatus.Withdrawn;
    }
}
