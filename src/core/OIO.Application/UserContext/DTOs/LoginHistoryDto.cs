namespace OIO.Application.UserContext.DTOs;

public sealed record LoginHistoryDto(
    Guid Id,
    string IpAddress,
    string UserAgent,
    DateTime LoginAt,
    string Status);