namespace OIO.Application.Context.PaymentContext.DTOs;

public sealed record PaymentMethodDto(
    Guid Id,
    string Type,
    string? Provider,
    string? LastFour,
    int? ExpiryMonth,
    int? ExpiryYear,
    string? HolderName,
    bool IsDefault,
    bool IsActive,
    DateTime CreatedAt);
