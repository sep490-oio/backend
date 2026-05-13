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
    string? TransferProofUrl,
    string? TransferNote,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    bool IsHighRisk = false,
    bool UserKycVerified = false);
