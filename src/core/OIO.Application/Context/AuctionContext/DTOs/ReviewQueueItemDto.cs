namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record ReviewQueueItemDto(
    Guid ItemId,
    Guid AuctionId,
    string Title,
    string Status,
    string Condition,
    Guid SellerId,
    Guid? AssignedAdminId,
    int ResubmissionCount,
    int MediaCount,
    DateTime? SubmittedAt,
    DateTime CreatedAt);

public sealed record ItemModerationReviewDto(
    Guid Id,
    string Action,
    Guid ReviewerId,
    string? Reason,
    string? OldStatus,
    string? NewStatus,
    DateTime CreatedAt);
