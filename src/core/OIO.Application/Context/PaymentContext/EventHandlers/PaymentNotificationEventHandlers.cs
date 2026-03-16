using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.PaymentContext.DomainEvents;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;

namespace OIO.Application.Context.PaymentContext.EventHandlers;

internal sealed class WalletCreditedNotificationHandler
    : INotificationHandler<WalletCreditedDomainEvent>
{
    private readonly ISender _sender;
    private readonly ILogger<WalletCreditedNotificationHandler> _logger;

    public WalletCreditedNotificationHandler(
        ISender sender,
        ILogger<WalletCreditedNotificationHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(WalletCreditedDomainEvent notification, CancellationToken cancellationToken)
    {
        var description = string.IsNullOrWhiteSpace(notification.Description)
            ? string.Empty
            : $" Noi dung: {notification.Description}.";

        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: notification.UserId.Value,
                NotificationType: "financial",
                EventType: "wallet_credited",
                Title: "Vi du duoc cong tien",
                Message:
                    $"Tai khoan cua ban vua duoc cong {NotificationDispatch.FormatAmount(notification.Amount)}. " +
                    $"So du hien tai: {NotificationDispatch.FormatAmount(notification.BalanceAfter)}.{description}",
                Priority: NotificationPriority.Normal,
                EntityType: "Wallet",
                EntityId: notification.WalletId.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    walletId = notification.WalletId.Value,
                    amount = notification.Amount,
                    balanceAfter = notification.BalanceAfter,
                    description = notification.Description
                })),
            cancellationToken);
    }
}

internal sealed class WalletDebitedNotificationHandler
    : INotificationHandler<WalletDebitedDomainEvent>
{
    private readonly ISender _sender;
    private readonly ILogger<WalletDebitedNotificationHandler> _logger;

    public WalletDebitedNotificationHandler(
        ISender sender,
        ILogger<WalletDebitedNotificationHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(WalletDebitedDomainEvent notification, CancellationToken cancellationToken)
    {
        var description = string.IsNullOrWhiteSpace(notification.Description)
            ? string.Empty
            : $" Noi dung: {notification.Description}.";

        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: notification.UserId.Value,
                NotificationType: "financial",
                EventType: "wallet_debited",
                Title: "Vi du bi tru tien",
                Message:
                    $"Tai khoan cua ban vua bi tru {NotificationDispatch.FormatAmount(notification.Amount)}. " +
                    $"So du hien tai: {NotificationDispatch.FormatAmount(notification.BalanceAfter)}.{description}",
                Priority: NotificationPriority.Normal,
                EntityType: "Wallet",
                EntityId: notification.WalletId.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    walletId = notification.WalletId.Value,
                    amount = notification.Amount,
                    balanceAfter = notification.BalanceAfter,
                    description = notification.Description
                })),
            cancellationToken);
    }
}

internal sealed class TransactionFailedNotificationHandler
    : INotificationHandler<TransactionFailedDomainEvent>
{
    private readonly ISender _sender;
    private readonly ILogger<TransactionFailedNotificationHandler> _logger;

    public TransactionFailedNotificationHandler(
        ISender sender,
        ILogger<TransactionFailedNotificationHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(TransactionFailedDomainEvent notification, CancellationToken cancellationToken)
    {
        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: notification.UserId.Value,
                NotificationType: "financial",
                EventType: "transaction_failed",
                Title: "Giao dich that bai",
                Message:
                    $"Giao dich {notification.TransactionType} cua ban da that bai voi gia tri " +
                    $"{NotificationDispatch.FormatAmount(notification.Amount, notification.Currency)}.",
                Priority: NotificationPriority.High,
                EntityType: "Transaction",
                EntityId: notification.TransactionId.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    transactionId = notification.TransactionId.Value,
                    transactionType = notification.TransactionType,
                    amount = notification.Amount,
                    currency = notification.Currency
                })),
            cancellationToken);
    }
}

internal sealed class WithdrawalCompletedNotificationHandler
    : INotificationHandler<WithdrawalCompletedDomainEvent>
{
    private readonly ISender _sender;
    private readonly ILogger<WithdrawalCompletedNotificationHandler> _logger;

    public WithdrawalCompletedNotificationHandler(
        ISender sender,
        ILogger<WithdrawalCompletedNotificationHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(WithdrawalCompletedDomainEvent notification, CancellationToken cancellationToken)
    {
        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: notification.UserId.Value,
                NotificationType: "financial",
                EventType: "withdrawal_completed",
                Title: "Rut tien thanh cong",
                Message:
                    $"Yeu cau rut tien cua ban da duoc xu ly thanh cong. So tien nhan ve: " +
                    $"{NotificationDispatch.FormatAmount(notification.NetAmount)}.",
                Priority: NotificationPriority.Normal,
                EntityType: "WithdrawalRequest",
                EntityId: notification.WithdrawalRequestId.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    withdrawalRequestId = notification.WithdrawalRequestId.Value,
                    netAmount = notification.NetAmount
                })),
            cancellationToken);
    }
}

internal sealed class WithdrawalRejectedNotificationHandler
    : INotificationHandler<WithdrawalRejectedDomainEvent>
{
    private readonly ISender _sender;
    private readonly ILogger<WithdrawalRejectedNotificationHandler> _logger;

    public WithdrawalRejectedNotificationHandler(
        ISender sender,
        ILogger<WithdrawalRejectedNotificationHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(WithdrawalRejectedDomainEvent notification, CancellationToken cancellationToken)
    {
        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: notification.UserId.Value,
                NotificationType: "financial",
                EventType: "withdrawal_rejected",
                Title: "Yeu cau rut tien bi tu choi",
                Message: $"Yeu cau rut tien cua ban bi tu choi. Ly do: {notification.Reason}",
                Priority: NotificationPriority.High,
                EntityType: "WithdrawalRequest",
                EntityId: notification.WithdrawalRequestId.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    withdrawalRequestId = notification.WithdrawalRequestId.Value,
                    amount = notification.Amount,
                    reason = notification.Reason,
                    rejectedBy = notification.RejectedBy.Value
                })),
            cancellationToken);
    }
}

internal sealed class InvoicePaidNotificationHandler
    : INotificationHandler<InvoicePaidDomainEvent>
{
    private readonly ISender _sender;
    private readonly ILogger<InvoicePaidNotificationHandler> _logger;

    public InvoicePaidNotificationHandler(
        ISender sender,
        ILogger<InvoicePaidNotificationHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(InvoicePaidDomainEvent notification, CancellationToken cancellationToken)
    {
        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: notification.BuyerId.Value,
                NotificationType: "financial",
                EventType: "invoice_paid",
                Title: "Thanh toan thanh cong",
                Message:
                    $"Hoa don cua ban da duoc thanh toan thanh cong voi tong gia tri " +
                    $"{NotificationDispatch.FormatAmount(notification.TotalAmount)}.",
                Priority: NotificationPriority.Normal,
                EntityType: "Invoice",
                EntityId: notification.InvoiceId.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    invoiceId = notification.InvoiceId.Value,
                    orderId = notification.OrderId,
                    totalAmount = notification.TotalAmount
                })),
            cancellationToken);
    }
}

internal sealed class EscrowReleasedNotificationHandler(
    IDbContext dbContext,
    ISender sender,
    ILogger<EscrowReleasedNotificationHandler> logger)
    : INotificationHandler<EscrowReleasedToSellerDomainEvent>
{
    public async Task Handle(EscrowReleasedToSellerDomainEvent notification, CancellationToken cancellationToken)
    {
        var order = await dbContext.Set<Order>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == OrderId.From(notification.OrderId), cancellationToken);

        if (order is null)
            return;

        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: order.SellerId.Value,
                NotificationType: "financial",
                EventType: "escrow_released",
                Title: "Tien giu da duoc giai ngan",
                Message:
                    $"Khoan tien giu cho don {order.OrderNumber.Value} da duoc giai ngan voi gia tri " +
                    $"{NotificationDispatch.FormatAmount(notification.Amount, notification.Currency)}.",
                Priority: NotificationPriority.High,
                EntityType: "Order",
                EntityId: order.Id.Value),
            cancellationToken);
    }
}

internal sealed class EscrowRefundedNotificationHandler(
    IDbContext dbContext,
    ISender sender,
    ILogger<EscrowRefundedNotificationHandler> logger)
    : INotificationHandler<EscrowRefundedToBuyerDomainEvent>
{
    public async Task Handle(EscrowRefundedToBuyerDomainEvent notification, CancellationToken cancellationToken)
    {
        var order = await dbContext.Set<Order>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == OrderId.From(notification.OrderId), cancellationToken);

        if (order is null)
            return;

        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: order.BuyerId.Value,
                NotificationType: "financial",
                EventType: "escrow_refunded",
                Title: "Tien giu da duoc hoan",
                Message:
                    $"Khoan tien giu cho don {order.OrderNumber.Value} da duoc hoan voi gia tri " +
                    $"{NotificationDispatch.FormatAmount(notification.Amount, notification.Currency)}.",
                Priority: NotificationPriority.High,
                EntityType: "Order",
                EntityId: order.Id.Value),
            cancellationToken);
    }
}
