namespace OIO.Application.Context.ModerationContext.DTOs;

public sealed record DisputeIntakeDto(
    Guid Id,
    string DisputeNumber,
    string Status,
    string? Domain,
    string? CaseType,
    string? PrimaryTargetType,
    Guid? OrderId,
    Guid? AuctionId,
    Guid? ShipmentId,
    Guid? WarehouseItemId,
    Guid? PaymentId,
    Guid ComplainantUserId,
    Guid? RespondentUserId,
    string Title,
    string? Description,
    DateTime CreatedAt);
