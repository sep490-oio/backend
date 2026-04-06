namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record ItemQuestionDto(
    Guid Id,
    Guid AskerId,
    string Question,
    string? Answer,
    DateTime? AnsweredAt,
    bool IsPublic,
    DateTime CreatedAt,
    string? AskerDisplayName = null,
    string? AnswererDisplayName = null);