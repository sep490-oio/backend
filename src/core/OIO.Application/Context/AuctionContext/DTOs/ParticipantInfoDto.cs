namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record ParticipantInfoDto(
    string? QualificationStatus,
    string? JoinStatus,
    string? DepositStatus,
    decimal? DepositAmount,
    string? DepositCurrency);
