using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Shared;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class AuctionDeposit : BaseEntity<AuctionDepositId>
{
    // Properties khớp 100% với Script DB
    public AuctionId AuctionId { get; private set; }
    public Guid UserId { get; private set; }
    public Money Amount { get; private set; }
    public Guid? TransactionId { get; private set; } // transaction_id
    public DepositStatus Status { get; private set; } // status (held, returned, etc.)
    public DateTime CreatedAt { get; private set; }   // created_at
    public DateTime? ReleasedAt { get; private set; } // released_at

    private AuctionDeposit() { } // Dành cho EF Core

    internal AuctionDeposit(
        AuctionDepositId id, 
        AuctionId auctionId, 
        Guid userId, 
        Money amount, 
        Guid? transactionId, 
        DateTime now) 
        : base(id)
    {
        AuctionId = auctionId;
        UserId = userId;
        Amount = amount;
        TransactionId = transactionId;
        Status = DepositStatus.Held; // Default 'held' theo script
        CreatedAt = now;
    }

    // --- Business Methods dựa trên DepositStatus Enum mới ---

    public void Return(DateTime now)
    {
        Status = DepositStatus.Returned;
        ReleasedAt = now;
    }

    public void Forfeit(DateTime now)
    {
        Status = DepositStatus.Forfeited;
        ReleasedAt = now;
    }

    public void ConvertToPayment(DateTime now)
    {
        Status = DepositStatus.ConvertedToPayment;
        ReleasedAt = now;
    }
}