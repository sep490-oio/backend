using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

namespace OIO.Application.Context.AuctionContext.Mappings;

internal static class AuctionSupportMappings
{
    public static AuctionParticipantDto ToDto(this AuctionParticipant participant)
    {
        return new AuctionParticipantDto(
            Id: participant.Id.Value,
            AuctionId: participant.AuctionId.Value,
            UserId: participant.UserId.Value,
            RoleInAuction: participant.RoleInAuction,
            JoinStatus: participant.JoinStatus.Id,
            QualificationStatus: participant.QualificationStatus.Id,
            JoinedAt: participant.JoinedAt,
            QualifiedAt: participant.QualifiedAt,
            RejectedReason: participant.RejectedReason);
    }

    public static SealedBidDto ToDto(this SealedBid sealedBid, decimal? revealedAmount = null)
    {
        return new SealedBidDto(
            Id: sealedBid.Id.Value,
            AuctionId: sealedBid.AuctionId.Value,
            BidderId: sealedBid.BidderId.Value,
            AmountEncrypted: sealedBid.AmountEncrypted,
            Status: sealedBid.Status.Id,
            CreatedAt: sealedBid.CreatedAt,
            RevealedAt: sealedBid.RevealedAt,
            RevealedBy: sealedBid.RevealedBy?.Value,
            RevealedAmount: revealedAmount);
    }

    public static AuctionEmergencyDto ToDto(this AuctionEmergency emergency)
    {
        return new AuctionEmergencyDto(
            Id: emergency.Id.Value,
            AuctionId: emergency.AuctionId.Value,
            TriggeredById: emergency.TriggeredById?.Value,
            TriggerSource: emergency.TriggerSource,
            Reason: emergency.Reason,
            Status: emergency.Status.Id,
            TriggeredAt: emergency.TriggeredAt,
            ResolvedAt: emergency.ResolvedAt);
    }
}
