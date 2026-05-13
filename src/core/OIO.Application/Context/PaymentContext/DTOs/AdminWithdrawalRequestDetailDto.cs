namespace OIO.Application.Context.PaymentContext.DTOs;

public sealed record AdminWithdrawalRequestDetailDto(
    Guid Id,
    Guid UserId,
    string? UserDisplayName,
    string? UserEmail,
    Guid WalletId,
    decimal Amount,
    decimal Fee,
    decimal NetAmount,
    string Status,
    string? BankName,
    string? AccountNumber,
    string? AccountHolder,
    string? RejectionReason,
    string? TransferProofUrl,
    string? TransferNote,
    Guid? ProcessedBy,
    string? ProcessedByDisplayName,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    bool IsHighRisk = false,
    bool UserKycVerified = false);
