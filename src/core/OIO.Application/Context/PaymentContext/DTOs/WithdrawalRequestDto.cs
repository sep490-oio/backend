namespace OIO.Application.Context.PaymentContext.DTOs;

public sealed record WithdrawalRequestDto(
    Guid Id,
    decimal Amount,
    decimal Fee,
    decimal NetAmount,
    string Status,
    string? BankName,
    string? AccountNumberMasked,
    string? AccountHolder,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? ProcessedAt);
