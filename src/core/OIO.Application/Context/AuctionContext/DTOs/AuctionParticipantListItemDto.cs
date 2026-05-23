using OIO.Domain.Context.AuctionContext.Enums;

namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record AuctionParticipantListItemDto(
    Guid AuctionId,
    Guid UserId,
    string? DisplayName,
    string JoinStatus,
    string QualificationStatus,
    DateTime RegisteredAt,
    DateTime? QualifiedAt,
    decimal? DepositAmount,
    string? DepositCurrency);
