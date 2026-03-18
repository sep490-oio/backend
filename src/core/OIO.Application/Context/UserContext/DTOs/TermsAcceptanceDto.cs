namespace OIO.Application.Context.UserContext.DTOs;

public sealed record TermsAcceptanceDto(
    Guid Id,
    DateTime AcceptedAt,
    string? IpAddress,
    string? UserAgent,
    TermsDocumentDto Document);