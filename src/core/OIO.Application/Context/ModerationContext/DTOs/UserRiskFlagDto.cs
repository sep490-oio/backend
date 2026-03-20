namespace OIO.Application.Context.ModerationContext.DTOs;

public sealed record UserRiskFlagDto(
    Guid Id,
    Guid UserId,
    string FlagType,
    string? Reason,
    string Severity,
    Guid? CreatedBy,
    DateTime CreatedAt);
