namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record AuctionEmergencyDto(
    Guid Id,
    Guid AuctionId,
    Guid? TriggeredById,
    string TriggerSource,
    string Reason,
    string Status,
    DateTime TriggeredAt,
    DateTime? ResolvedAt);
