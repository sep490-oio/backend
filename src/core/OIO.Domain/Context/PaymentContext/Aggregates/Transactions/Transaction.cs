using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.Aggregates.PaymentMethods;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.PaymentContext.Aggregates.Transactions;

public sealed class Transaction : AggregateRoot<TransactionId>, ICreatedAtEntity
{
    private readonly List<AuctionDeposit> _auctionDeposits = [];
    private readonly List<Wallet> _wallets;
    
    public TransactionNumber TransactionNumber { get; private set; }
    public OrderId? OrderId { get; private set; }
    public UserId UserId { get; private set; }
    public PaymentMethodId? PaymentMethodId { get; private set; }
    public TransactionType Type { get; private set; }
    public Money Amount { get; private set; }
    public decimal Fee { get; private set; }
    public Money NetAmount { get; private set; }
    public string Currency { get; private set; }
    public TransactionStatus Status { get; private set; }
    public GatewayInfo Gateway { get; private set; }
    public string? Description { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public PaymentMethod? PaymentMethod { get; private set; }
    public IReadOnlyCollection<AuctionDeposit> AuctionDeposits => _auctionDeposits.AsReadOnly();
    public IReadOnlyCollection<Wallet> Wallets => _wallets.AsReadOnly();

    private Transaction() { }
}