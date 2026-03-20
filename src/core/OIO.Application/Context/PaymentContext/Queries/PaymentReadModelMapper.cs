using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Domain.Context.PaymentContext.Aggregates.Escrows;
using OIO.Domain.Context.PaymentContext.Aggregates.PaymentMethods;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Aggregates.Withdrawals;
using OIO.Domain.Context.PaymentContext.Enums;

namespace OIO.Application.Context.PaymentContext.Queries;

internal static class PaymentReadModelMapper
{
    public static WalletSummaryDto ToSummaryDto(Wallet wallet)
        => new(
            WalletId: wallet.Id.Value,
            Currency: wallet.WalletFunds.Currency.Id,
            AvailableBalance: wallet.WalletFunds.BalanceAmount,
            PendingBalance: wallet.WalletFunds.PendingBalanceAmount,
            TotalBalance: wallet.WalletFunds.BalanceAmount + wallet.WalletFunds.PendingBalanceAmount,
            IsActive: wallet.IsActive,
            UpdatedAt: wallet.ModifiedAt ?? wallet.CreatedAt);

    public static PaymentMethodDto ToDto(PaymentMethod paymentMethod)
        => new(
            Id: paymentMethod.Id.Value,
            Type: paymentMethod.Type.Id,
            Provider: paymentMethod.Provider,
            LastFour: paymentMethod.Card.LastFour,
            ExpiryMonth: paymentMethod.Card.ExpiryMonth,
            ExpiryYear: paymentMethod.Card.ExpiryYear,
            HolderName: paymentMethod.Card.HolderName,
            IsDefault: paymentMethod.IsDefault,
            IsActive: paymentMethod.IsActive,
            CreatedAt: paymentMethod.CreatedAt,
            MaskedCardNumber: paymentMethod.MaskedCardNumber,
            VnPayCardType: paymentMethod.VnPayCardType,
            BankCode: paymentMethod.BankCode);

    public static WithdrawalRequestDto ToDto(WithdrawalRequest withdrawal)
        => new(
            Id: withdrawal.Id.Value,
            Amount: withdrawal.Amount,
            Fee: withdrawal.Fee,
            NetAmount: withdrawal.NetAmount,
            Status: withdrawal.Status.Id,
            BankName: withdrawal.BankAccount.BankName,
            AccountNumberMasked: MaskAccountNumber(withdrawal.BankAccount.AccountNumber),
            AccountHolder: withdrawal.BankAccount.AccountHolder,
            RejectionReason: withdrawal.RejectionReason,
            CreatedAt: withdrawal.CreatedAt,
            ProcessedAt: withdrawal.ProcessedAt);

    public static AdminWithdrawalRequestDetailDto ToAdminDetailDto(WithdrawalRequest withdrawal)
        => new(
            Id: withdrawal.Id.Value,
            UserId: withdrawal.UserId.Value,
            WalletId: withdrawal.WalletId.Value,
            Amount: withdrawal.Amount,
            Fee: withdrawal.Fee,
            NetAmount: withdrawal.NetAmount,
            Status: withdrawal.Status.Id,
            BankName: withdrawal.BankAccount.BankName,
            AccountNumber: withdrawal.BankAccount.AccountNumber,
            AccountHolder: withdrawal.BankAccount.AccountHolder,
            RejectionReason: withdrawal.RejectionReason,
            ProcessedBy: withdrawal.ProcessedBy?.Value,
            CreatedAt: withdrawal.CreatedAt,
            ProcessedAt: withdrawal.ProcessedAt);

    public static PaymentTransactionDto ToDto(Transaction transaction)
        => new(
            Id: transaction.Id.Value,
            TransactionNumber: transaction.TransactionNumber.Value,
            UserId: transaction.UserId.Value,
            OrderId: transaction.OrderId?.Value,
            Type: transaction.Type.Id,
            Amount: transaction.Amount.Amount,
            Fee: transaction.Fee,
            NetAmount: transaction.NetAmount.Amount,
            Currency: transaction.Currency,
            Status: transaction.Status.Id,
            GatewayProvider: transaction.Gateway.Provider,
            Description: transaction.Description,
            CreatedAt: transaction.CreatedAt,
            ProcessedAt: transaction.ProcessedAt);

    public static EscrowDto ToDto(Escrow escrow)
        => new(
            Id: escrow.Id.Value,
            OrderId: escrow.OrderId.Value,
            BuyerId: escrow.Order.BuyerId.Value,
            SellerId: escrow.Order.SellerId.Value,
            Amount: escrow.Amount.Amount,
            Currency: escrow.Currency,
            Status: escrow.Status.Id,
            HoldTransactionId: escrow.HoldTransactionId?.Value,
            CreatedAt: escrow.HeldAt,
            ReleasedAt: escrow.Status == EscrowStatus.RefundedToBuyer ? null : escrow.ReleasedAt,
            RefundedAt: escrow.Status == EscrowStatus.RefundedToBuyer ? escrow.ReleasedAt : null);

    public static EscrowDetailDto ToDetailDto(Escrow escrow)
        => new(
            Id: escrow.Id.Value,
            OrderId: escrow.OrderId.Value,
            BuyerId: escrow.Order.BuyerId.Value,
            SellerId: escrow.Order.SellerId.Value,
            Amount: escrow.Amount.Amount,
            Currency: escrow.Currency,
            Status: escrow.Status.Id,
            HoldTransactionId: escrow.HoldTransactionId?.Value,
            ReleaseTransactionId: escrow.ReleaseTransactionId?.Value,
            ReleasedTo: escrow.ReleasedTo.Id,
            CreatedAt: escrow.HeldAt,
            ReleasedAt: escrow.ReleasedAt,
            ReleaseEvents: escrow.ReleaseEvents
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new EscrowReleaseEventDto(
                    Id: x.Id.Value,
                    ReleaseType: x.ReleaseType.Id,
                    TriggerSourceType: x.TriggerSourceType,
                    TriggerSourceId: x.TriggerSourceId,
                    Amount: x.Amount,
                    CreatedBy: x.CreatedBy?.Value,
                    CreatedAt: x.CreatedAt))
                .ToList());

    public static WalletTransactionDto ToDto(WalletTransaction walletTransaction, string currency)
    {
        var (referenceType, referenceId) = ResolveReference(walletTransaction);

        return new WalletTransactionDto(
            Id: walletTransaction.Id.Value,
            Type: walletTransaction.Type.Id,
            Amount: walletTransaction.Amount,
            Currency: currency,
            BalanceBefore: walletTransaction.BalanceBefore,
            BalanceAfter: walletTransaction.BalanceAfter,
            Description: walletTransaction.Description,
            ReferenceType: referenceType,
            ReferenceId: referenceId,
            CreatedAt: walletTransaction.CreatedAt);
    }

    private static (string? ReferenceType, Guid? ReferenceId) ResolveReference(WalletTransaction walletTransaction)
    {
        if (walletTransaction.Transaction?.OrderId is not null)
            return ("order", walletTransaction.Transaction.OrderId.Value.Value);

        if (walletTransaction.Transaction?.AuctionId is not null)
            return ("deposit", walletTransaction.Transaction.AuctionId.Value.Value);

        if (walletTransaction.Description?.Contains("Auction deposit", StringComparison.OrdinalIgnoreCase) == true ||
            walletTransaction.Transaction?.Description?.Contains("[AuctionDeposit]", StringComparison.OrdinalIgnoreCase) == true)
        {
            return ("deposit", ParseAuctionId(walletTransaction.Transaction?.Description) ?? walletTransaction.TransactionId?.Value);
        }

        if (walletTransaction.Description?.Contains("Withdrawal", StringComparison.OrdinalIgnoreCase) == true)
            return ("withdrawal", walletTransaction.TransactionId?.Value);

        if (walletTransaction.Description?.Contains("Escrow", StringComparison.OrdinalIgnoreCase) == true)
            return ("escrow", walletTransaction.Transaction?.OrderId?.Value ?? walletTransaction.TransactionId?.Value);

        if (walletTransaction.Transaction?.Description?.Contains("[WalletTopUp]", StringComparison.OrdinalIgnoreCase) == true ||
            walletTransaction.Description?.Contains("wallet top-up", StringComparison.OrdinalIgnoreCase) == true)
        {
            return ("transaction", walletTransaction.TransactionId?.Value);
        }

        return walletTransaction.TransactionId.HasValue
            ? ("transaction", walletTransaction.TransactionId.Value.Value)
            : (null, null);
    }

    private static Guid? ParseAuctionId(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        const string marker = "AuctionId:";
        var index = description.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return null;

        var raw = description[(index + marker.Length)..].Trim();
        return Guid.TryParse(raw, out var parsed) ? parsed : null;
    }

    public static string? MaskAccountNumber(string? accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
            return accountNumber;

        var trimmed = accountNumber.Trim();
        if (trimmed.Length <= 4)
            return trimmed;

        return $"{new string('*', trimmed.Length - 4)}{trimmed[^4..]}";
    }
}
