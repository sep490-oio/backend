namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record AuctionParticipantDto(
    Guid Id,
    Guid AuctionId,
    Guid UserId,
    string RoleInAuction,
    string JoinStatus,
    string QualificationStatus,
    DateTime JoinedAt,
    DateTime? QualifiedAt,
    string? RejectedReason);
