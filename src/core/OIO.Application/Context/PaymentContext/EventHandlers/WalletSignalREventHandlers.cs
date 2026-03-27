using MediatR;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.UserContext.Hubs;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.DomainEvents;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;

namespace OIO.Application.Context.PaymentContext.EventHandlers;

/// <summary>
/// Pushes wallet balance changes to the UserHub for real-time updates.
/// </summary>
internal sealed class WalletSignalREventHandlers :
    INotificationHandler<WalletCreditedDomainEvent>,
    INotificationHandler<WalletDebitedDomainEvent>,
    INotificationHandler<WalletHeldDomainEvent>,
    INotificationHandler<WalletUnheldDomainEvent>
{
    private readonly IDbContext _dbContext;
    private readonly IUserNotificationService _userNotificationService;

    public WalletSignalREventHandlers(
        IDbContext dbContext,
        IUserNotificationService userNotificationService)
    {
        _dbContext = dbContext;
        _userNotificationService = userNotificationService;
    }

    public async Task Handle(WalletCreditedDomainEvent notification, CancellationToken ct)
        => await NotifyWalletUpdate(notification.WalletId, notification.UserId.Value, "credit", notification.Amount, ct);

    public async Task Handle(WalletDebitedDomainEvent notification, CancellationToken ct)
        => await NotifyWalletUpdate(notification.WalletId, notification.UserId.Value, "debit", notification.Amount, ct);

    public async Task Handle(WalletHeldDomainEvent notification, CancellationToken ct)
        => await NotifyWalletUpdate(notification.WalletId, notification.UserId.Value, "hold", notification.Amount, ct);

    public async Task Handle(WalletUnheldDomainEvent notification, CancellationToken ct)
        => await NotifyWalletUpdate(notification.WalletId, notification.UserId.Value, "release", notification.Amount, ct);

    private async Task NotifyWalletUpdate(WalletId walletId, Guid userId, string transactionType, decimal amount, CancellationToken ct)
    {
        var wallet = await _dbContext.Set<Wallet>()
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == walletId, ct);

        if (wallet is null) return;

        var notification = new WalletUpdatedNotification(
            AvailableBalance: wallet.WalletFunds.BalanceAmount,
            PendingBalance: wallet.WalletFunds.PendingBalanceAmount,
            TotalBalance: wallet.WalletFunds.BalanceAmount + wallet.WalletFunds.PendingBalanceAmount,
            TransactionType: transactionType,
            Amount: amount,
            Currency: wallet.WalletFunds.Currency.Id);

        await _userNotificationService.NotifyWalletUpdatedAsync(userId, notification);
    }
}
