using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Domain.Context.PaymentContext.Aggregates.Escrows;
using OIO.Domain.Context.PaymentContext.Aggregates.PaymentMethods;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Aggregates.Withdrawals;
using OIO.Domain.Context.PaymentContext.Descriptions;
using OIO.Domain.Context.PaymentContext.Enums;

namespace OIO.Application.Context.PaymentContext.Queries;

internal static class PaymentReadModelMapper
{
    public static WalletSummaryDto ToSummaryDto(this Wallet wallet)
        => new(
            WalletId: wallet.Id.Value,
            Currency: wallet.WalletFunds.Currency.Id,
            AvailableBalance: wallet.WalletFunds.BalanceAmount,
            PendingBalance: wallet.WalletFunds.PendingBalanceAmount,
            TotalBalance: wallet.WalletFunds.BalanceAmount + wallet.WalletFunds.PendingBalanceAmount,
            IsActive: wallet.IsActive,
            UpdatedAt: wallet.ModifiedAt ?? wallet.CreatedAt);

    public static PaymentMethodDto ToDto(this PaymentMethod paymentMethod)
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

    public static WithdrawalRequestDto ToDto(this WithdrawalRequest withdrawal)
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
            TransferProofUrl: withdrawal.TransferProofUrl,
            TransferNote: withdrawal.TransferNote,
            CreatedAt: withdrawal.CreatedAt,
            ProcessedAt: withdrawal.ProcessedAt);

    public static AdminWithdrawalRequestDetailDto ToAdminDetailDto(this WithdrawalRequest withdrawal)
        => new(
            Id: withdrawal.Id.Value,
            UserId: withdrawal.UserId.Value,
            UserDisplayName: null,
            UserEmail: null,
            WalletId: withdrawal.WalletId.Value,
            Amount: withdrawal.Amount,
            Fee: withdrawal.Fee,
            NetAmount: withdrawal.NetAmount,
            Status: withdrawal.Status.Id,
            BankName: withdrawal.BankAccount.BankName,
            AccountNumber: withdrawal.BankAccount.AccountNumber,
            AccountHolder: withdrawal.BankAccount.AccountHolder,
            RejectionReason: withdrawal.RejectionReason,
            TransferProofUrl: withdrawal.TransferProofUrl,
            TransferNote: withdrawal.TransferNote,
            ProcessedBy: withdrawal.ProcessedBy?.Value,
            ProcessedByDisplayName: null,
            CreatedAt: withdrawal.CreatedAt,
            ProcessedAt: withdrawal.ProcessedAt,
            IsHighRisk: withdrawal.Amount > 10000000m,
            UserKycVerified: false);

    public static PaymentTransactionDto ToDto(
        this Transaction transaction,
        string? userDisplayName = null,
        string? orderNumber = null,
        string? auctionItemTitle = null,
        Guid? processedBy = null,
        string? processedByDisplayName = null,
        string? processNote = null)
        => new(
            Id: transaction.Id.Value,
            TransactionNumber: transaction.TransactionNumber.Value,
            UserId: transaction.UserId.Value,
            UserDisplayName: userDisplayName,
            OrderId: transaction.OrderId?.Value,
            OrderNumber: orderNumber,
            AuctionId: transaction.AuctionId?.Value,
            AuctionItemTitle: auctionItemTitle,
            Type: transaction.Type.Id,
            Amount: transaction.Amount.Amount,
            Fee: transaction.Fee,
            NetAmount: transaction.NetAmount.Amount,
            Currency: transaction.Currency,
            Status: transaction.Status.Id,
            GatewayProvider: transaction.Gateway.Provider,
            Description: transaction.Description,
            CreatedAt: transaction.CreatedAt,
            ProcessedAt: transaction.ProcessedAt,
            ProcessedBy: processedBy,
            ProcessedByDisplayName: processedByDisplayName,
            ProcessNote: processNote);

    public static EscrowDto ToDto(
        this Escrow escrow,
        string? orderNumber = null,
        string? buyerDisplayName = null,
        string? sellerDisplayName = null,
        string? auctionItemTitle = null)
        => new(
            Id: escrow.Id.Value,
            OrderId: escrow.OrderId.Value,
            OrderNumber: orderNumber,
            BuyerId: escrow.Order.BuyerId.Value,
            BuyerDisplayName: buyerDisplayName,
            SellerId: escrow.Order.SellerId.Value,
            SellerDisplayName: sellerDisplayName,
            AuctionItemTitle: auctionItemTitle,
            Amount: escrow.Amount.Amount,
            Currency: escrow.Currency,
            Status: escrow.Status.Id,
            HoldTransactionId: escrow.HoldTransactionId?.Value,
            CreatedAt: escrow.HeldAt,
            ReleasedAt: escrow.Status == EscrowStatus.RefundedToBuyer ? null : escrow.ReleasedAt,
            RefundedAt: escrow.Status == EscrowStatus.RefundedToBuyer ? escrow.ReleasedAt : null,
            ReleaseEvents: escrow.ReleaseEvents
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new EscrowReleaseEventDto(
                    Id: x.Id.Value,
                    ReleaseType: x.ReleaseType.Id,
                    TriggerSourceType: x.TriggerSourceType,
                    TriggerSourceId: x.TriggerSourceId,
                    Amount: x.Amount,
                    CreatedBy: x.CreatedBy?.Value,
                    CreatedByDisplayName: null,
                    CreatedAt: x.CreatedAt))
                .ToList(),
            IsDisputed: escrow.Order?.Status == OIO.Domain.Context.OrderContext.Enums.OrderStatus.Disputed);

    public static EscrowDetailDto ToDetailDto(this Escrow escrow)
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
                    CreatedByDisplayName: null,
                    CreatedAt: x.CreatedAt))
                .ToList());

    public static WalletTransactionDto ToDto(this WalletTransaction walletTransaction, string currency)
    {
        var (referenceType, referenceId) = ResolveReference(walletTransaction);
        var sourceStatus = walletTransaction.Transaction?.Status.Id;
        var eventType = ResolveEventType(walletTransaction, referenceType);
        var reasonCode = ResolveReasonCode(walletTransaction, eventType);
        var ledgerStatus = ResolveLedgerStatus(walletTransaction, sourceStatus);

        return new WalletTransactionDto(
            Id: walletTransaction.Id.Value,
            Type: walletTransaction.Type.Id,
            Amount: walletTransaction.Amount,
            // Legacy field — kept for backward-compat with older clients. New
            // clients should read <see cref="WalletTransactionDto.SourceStatus"/>
            // (same value) or <see cref="WalletTransactionDto.LedgerStatus"/>
            // (ledger-side state, never null).
            Status: sourceStatus,
            Currency: currency,
            BalanceBefore: walletTransaction.BalanceBefore,
            BalanceAfter: walletTransaction.BalanceAfter,
            Description: walletTransaction.Description,
            ReferenceType: referenceType,
            ReferenceId: referenceId,
            CreatedAt: walletTransaction.CreatedAt,
            LedgerStatus: ledgerStatus,
            SourceStatus: sourceStatus,
            EventType: eventType,
            ReasonCode: reasonCode,
            ReferenceNumber: walletTransaction.Transaction?.TransactionNumber.Value,
            ReferenceTitle: null);
    }

    /// <summary>
    /// Map a wallet ledger row to one of the canonical FE business event types.
    /// Order matters: prefer the most specific signal (real Order/Auction FK)
    /// before falling back to <see cref="WalletTransaction.Type"/> + reference
    /// hints. Description sniffing is the last resort and only runs when
    /// nothing structured is available (legacy data).
    /// </summary>
    private static string ResolveEventType(WalletTransaction wt, string? referenceType)
    {
        var typeId = wt.Type.Id;

        // Withdrawal flow — easiest to detect via description marker the
        // wallet aggregate writes (no FK on the wallet ledger today).
        if (wt.Description?.Contains(LedgerMarkers.Withdrawal, StringComparison.OrdinalIgnoreCase) == true)
        {
            // Hold = funds reserved when withdrawal is requested.
            // Debit = funds actually leaving the platform on approval.
            return typeId == "hold" ? "withdrawal_hold" : "withdrawal_release";
        }

        // Platform fee deduction — structural check on Transaction.Type.
        // Must run BEFORE order-reference fallback so fee debits linked to
        // orders (e.g. seller commission after buyer-win dispute) are correctly
        // classified as "fee" instead of "order_payment".
        if (wt.Transaction?.Type == TransactionType.Fee && typeId == "debit")
            return "fee";

        // Auction deposit lifecycle: Hold → deposit reservation, Release → refund
        // back into the available balance after the auction settles for a
        // non-winner / cancelled / no-reserve case.
        // A Credit on a deposit-referenced transaction is the gateway FUNDING of the
        // deposit: when the wallet is empty, the VNPay deposit flow first Credits the
        // wallet (money in) and then immediately Holds it. That Credit is incoming
        // money — NOT a refund — so it is surfaced as a wallet top-up. (A real deposit
        // refund is a Release/Unhold, handled above.) Debit on an auction ref = deposit
        // being consumed by an order payment (rare here — usually Order FK is set).
        if (referenceType == "deposit")
        {
            return typeId switch
            {
                "hold" => "auction_deposit_hold",
                "release" => "auction_deposit_refund",
                "credit" => "wallet_top_up",
                _ => "auction_deposit_hold",
            };
        }

        // Order-linked: Debit = checkout payment, Credit = refund (return /
        // cancellation / dispute resolution favoring buyer).
        if (referenceType == "order")
        {
            return typeId switch
            {
                "credit" => "order_refund",
                "debit" => "order_payment",
                _ => "order_payment",
            };
        }

        // Top-up: explicit marker in transaction description, OR a Credit row
        // with no reference (gateway → wallet).
        var topUpMarker =
            wt.Transaction?.Description?.Contains(LedgerTags.Bracket(LedgerTags.WalletTopUp), StringComparison.OrdinalIgnoreCase) == true ||
            wt.Description?.Contains(LedgerMarkers.WalletTopUp, StringComparison.OrdinalIgnoreCase) == true;
        if (topUpMarker || (typeId == "credit" && referenceType is null))
            return "wallet_top_up";

        // Seller payout — escrow released to seller's wallet.
        if (referenceType == "escrow" && typeId == "credit")
            return "seller_payout";

        // Platform fee deduction — legacy string-match fallback for rows
        // that lack a Transaction FK (pre-revamp data). The structural
        // Transaction.Type == Fee check above handles all new rows.
        if (wt.Transaction is null
            && wt.Description?.Contains(LedgerMarkers.Fee, StringComparison.OrdinalIgnoreCase) == true
            && typeId == "debit")
            return "fee";

        // Generic fallback so the FE always has a key (no null event type).
        return typeId switch
        {
            "credit" => "wallet_top_up",
            "debit" => "order_payment",
            "hold" => "auction_deposit_hold",
            "release" => "auction_deposit_refund",
            _ => "wallet_top_up",
        };
    }

    /// <summary>
    /// Stable i18n key the FE renders. Today this mirrors <paramref name="eventType"/>
    /// 1:1, but the field is split out so we can introduce finer-grained reasons
    /// (e.g. <c>auction_sold_non_winner_refund</c> vs <c>reserve_not_met_refund</c>)
    /// without breaking the event-type taxonomy. Legacy rows that we couldn't
    /// classify confidently are tagged <c>legacy_unknown</c>.
    /// </summary>
    private static string ResolveReasonCode(WalletTransaction wt, string eventType)
    {
        // No structured signal AND no transaction FK = legacy / pre-revamp row.
        // Tag it so dashboards can flag the cohort for backfill.
        if (wt.Transaction is null
            && wt.Description is null
            && string.IsNullOrEmpty(eventType))
        {
            return "legacy_unknown";
        }
        return eventType;
    }

    /// <summary>
    /// Wallet ledger entries are append-only and immediately durable, so any
    /// row that exists is conceptually <c>posted</c>. We only override that
    /// when the originating payment transaction is in a non-final state
    /// (pending) or terminal-failure state (failed / reversed) so the UI can
    /// surface the upstream condition.
    /// </summary>
    private static string ResolveLedgerStatus(WalletTransaction wt, string? sourceStatus)
    {
        if (string.IsNullOrEmpty(sourceStatus))
            return "posted";

        // Map common payment-transaction statuses to ledger-side semantics.
        return sourceStatus.ToLowerInvariant() switch
        {
            "completed" or "succeeded" or "released" => "posted",
            "pending" or "processing" or "initiated" => "pending",
            "failed" or "rejected" or "expired" => "failed",
            "reversed" or "refunded" or "cancelled" => "reversed",
            _ => "posted",
        };
    }

    private static (string? ReferenceType, Guid? ReferenceId) ResolveReference(WalletTransaction walletTransaction)
    {
        if (walletTransaction.Transaction?.OrderId is not null)
            return ("order", walletTransaction.Transaction.OrderId.Value.Value);

        if (walletTransaction.Transaction?.AuctionId is not null)
            return ("deposit", walletTransaction.Transaction.AuctionId.Value.Value);

        if (walletTransaction.Description?.Contains(LedgerMarkers.AuctionDeposit, StringComparison.OrdinalIgnoreCase) == true ||
            walletTransaction.Transaction?.Description?.Contains(LedgerTags.Bracket(LedgerTags.AuctionDeposit), StringComparison.OrdinalIgnoreCase) == true)
        {
            // Try structured marker first (Transaction.Description), then fall
            // back to the WalletTransaction.Description which uses the format
            // "Auction deposit from wallet for auction {guid}".
            var auctionId = ParseAuctionId(walletTransaction.Transaction?.Description)
                            ?? ParseAuctionIdFromSuffix(walletTransaction.Description)
                            ?? walletTransaction.TransactionId?.Value;
            return ("deposit", auctionId);
        }

        if (walletTransaction.Description?.Contains(LedgerMarkers.Withdrawal, StringComparison.OrdinalIgnoreCase) == true)
            return ("withdrawal", walletTransaction.TransactionId?.Value);

        if (walletTransaction.Description?.Contains(LedgerMarkers.Escrow, StringComparison.OrdinalIgnoreCase) == true)
            return ("escrow", walletTransaction.Transaction?.OrderId?.Value ?? walletTransaction.TransactionId?.Value);

        if (walletTransaction.Transaction?.Description?.Contains(LedgerTags.Bracket(LedgerTags.WalletTopUp), StringComparison.OrdinalIgnoreCase) == true ||
            walletTransaction.Description?.Contains(LedgerMarkers.WalletTopUp, StringComparison.OrdinalIgnoreCase) == true)
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

        const string marker = LedgerMarkers.AuctionIdMarker;
        var index = description.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return null;

        var raw = description[(index + marker.Length)..].Trim();
        return Guid.TryParse(raw, out var parsed) ? parsed : null;
    }

    /// <summary>
    /// Fallback parser for wallet-deposit descriptions that embed the auction
    /// GUID at the end of the string (e.g. "Auction deposit from wallet for
    /// auction 01970ce3-..."). Tries to parse the last whitespace-delimited
    /// token as a GUID.
    /// </summary>
    private static Guid? ParseAuctionIdFromSuffix(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        // The description format: "... for auction {guid}"
        const string marker = LedgerMarkers.ForAuctionMarker;
        var index = description.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
        {
            // Only accept the token immediately after the marker as the auction id.
            // (The previous "last whitespace token is a GUID" heuristic was dropped — any
            // trailing GUID in any description would have parsed as an auction id.)
            var raw = description[(index + marker.Length)..].Trim();
            var firstToken = raw.Split(' ', 2)[0];
            if (Guid.TryParse(firstToken, out var parsed))
                return parsed;
        }

        return null;
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
