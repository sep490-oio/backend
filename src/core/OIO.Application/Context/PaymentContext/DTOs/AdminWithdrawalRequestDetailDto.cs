namespace OIO.Application.Context.PaymentContext.DTOs;

public sealed record AdminWithdrawalRequestDetailDto(
    Guid Id,
    Guid UserId,
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
    DateTime CreatedAt,
    DateTime? ProcessedAt);
