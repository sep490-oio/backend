using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using static OIO.Application.Context.OrderContext.Queries.Admin.GetCompletedAuctions.GetCompletedAuctionsQueryHandler;

namespace OIO.Application.Context.PaymentContext.Queries.Admins.GetAdminTransactions;

public record AdminTransactionFilterParameters : PagedParameters
{
    public string? Status { get; init; }
    public string? Type { get; init; }
    public Guid? UserId { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? AuctionId { get; init; }
    public DateTimeOffset? FromDate { get; init; }
    public DateTimeOffset? ToDate { get; init; }
    public string? SearchTerm { get; init; }
    public string? GatewayProvider { get; init; }
}

public sealed record GetAdminTransactionsQuery(
    AdminTransactionFilterParameters Parameters) : IQuery<PagedList<PaymentTransactionDto>>;

internal sealed class GetAdminTransactionsQueryHandler
    : IQueryHandler<GetAdminTransactionsQuery, PagedList<PaymentTransactionDto>>
{
    private readonly IDbContext _dbContext;

    public GetAdminTransactionsQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedList<PaymentTransactionDto>, Error>> Handle(
        GetAdminTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var query = _dbContext.Set<Transaction>()
            .AsNoTracking()
            .AsQueryable();

        if (parameters.UserId.HasValue)
            query = query.Where(x => x.UserId == UserId.From(parameters.UserId.Value));

        if (parameters.OrderId.HasValue)
            query = query.Where(x => x.OrderId != null && x.OrderId.Value == OrderId.From(parameters.OrderId.Value));

        if (parameters.AuctionId.HasValue)
            query = query.Where(x => x.AuctionId != null && x.AuctionId.Value == AuctionId.From(parameters.AuctionId.Value));

        if (parameters.FromDate.HasValue)
            query = query.Where(x => x.CreatedAt >= parameters.FromDate.Value);

        if (parameters.ToDate.HasValue)
            query = query.Where(x => x.CreatedAt <= parameters.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.Trim().ToLower();
            query = query.Where(x => x.TransactionNumber.Value.ToLower().Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var status = TransactionStatus.FromId(parameters.Status);
            if (status.HasNoValue)
                return Error.Validation("status", "Transaction.InvalidStatus", "Unsupported transaction status.");

            query = query.Where(x => x.Status.Id == status.Value.Id);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Type))
        {
            var type = TransactionType.FromId(parameters.Type);
            if (type.HasNoValue)
                return Error.Validation("type", "Transaction.InvalidType", "Unsupported transaction type.");

            query = query.Where(x => x.Type.Id == type.Value.Id);
        }

        if (!string.IsNullOrWhiteSpace(parameters.GatewayProvider))
        {
            var provider = parameters.GatewayProvider.Trim().ToLower();
            if (provider == "wallet" || provider == "system")
            {
                query = query.Where(x => string.IsNullOrEmpty(x.Gateway.Provider) || x.Gateway.Provider.ToLower() == "system");
            }
            else
            {
                query = query.Where(x => x.Gateway.Provider != null && x.Gateway.Provider.ToLower() == provider);
            }
        }

        query = query.OrderByDescending(x => x.CreatedAt);

        var count = await query.CountAsync(cancellationToken);

        // Project basic DTO first (no navigation JOINs — keeps the SQL simple).
        var items = await query
            .Select(x => x.ToDto())
            .ToPagedListAsync(count, parameters, cancellationToken);

        // ── Batch-enrich with display names ──────────────────────────────
        var userIds = items.Items
            .Select(t => UserId.From(t.UserId))
            .Distinct()
            .ToList();

        var users = userIds.Count > 0
            ? await _dbContext.Set<User>()
                .AsNoTracking()
                .Include(u => u.SellerProfile)
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync(cancellationToken)
            : [];
        var usersById = users.ToDictionary(u => u.Id.Value);

        // Resolve order numbers for order-linked transactions.
        var orderIds = items.Items
            .Where(t => t.OrderId.HasValue)
            .Select(t => OrderId.From(t.OrderId!.Value))
            .Distinct()
            .ToList();

        var orderNumbers = orderIds.Count > 0
            ? await _dbContext.Set<Order>()
                .AsNoTracking()
                .Where(o => orderIds.Contains(o.Id))
                .Select(o => new { Id = o.Id.Value, Number = o.OrderNumber.Value })
                .ToListAsync(cancellationToken)
            : [];
        var orderNumberById = orderNumbers.ToDictionary(o => o.Id, o => o.Number);

        // Resolve auction item titles for auction-linked transactions.
        var auctionIds = items.Items
            .Where(t => t.AuctionId.HasValue)
            .Select(t => AuctionId.From(t.AuctionId!.Value))
            .Distinct()
            .ToList();

        var auctionTitles = auctionIds.Count > 0
            ? await _dbContext.Set<Domain.Context.AuctionContext.Aggregates.Auctions.Auction>()
                .AsNoTracking()
                .Where(a => auctionIds.Contains(a.Id))
                .Select(a => new { Id = a.Id.Value, Title = a.Item.Title.Value })
                .ToListAsync(cancellationToken)
            : [];
        var titleByAuctionId = auctionTitles.ToDictionary(a => a.Id, a => a.Title);

        var withdrawalTxns = items.Items
            .Where(t => t.Type == TransactionType.Withdrawal.Id)
            .ToList();

        var withdrawRequestsByTxnId = new Dictionary<Guid, Domain.Context.PaymentContext.Aggregates.Withdrawals.WithdrawalRequest>();
        if (withdrawalTxns.Count > 0)
        {
            var userIdsForWithdrawals = withdrawalTxns.Select(t => UserId.From(t.UserId)).Distinct().ToList();
            var minDate = withdrawalTxns.Min(t => t.CreatedAt).AddMinutes(-2);
            var maxDate = withdrawalTxns.Max(t => t.CreatedAt).AddMinutes(2);

            var wReqs = await _dbContext.Set<Domain.Context.PaymentContext.Aggregates.Withdrawals.WithdrawalRequest>()
                .AsNoTracking()
                .Where(w => userIdsForWithdrawals.Contains(w.UserId) && w.CreatedAt >= minDate && w.CreatedAt <= maxDate)
                .ToListAsync(cancellationToken);

            foreach (var tx in withdrawalTxns)
            {
                var req = wReqs.FirstOrDefault(w => w.UserId.Value == tx.UserId && w.Amount == tx.Amount && Math.Abs((w.CreatedAt - tx.CreatedAt).TotalSeconds) < 10);
                if (req != null)
                {
                    withdrawRequestsByTxnId[tx.Id] = req;
                    if (req.ProcessedBy != null)
                        userIds.Add(req.ProcessedBy.Value); // Ensure admin is in userIds to fetch name later if not already there
                }
            }
        }

        // Fetch escrows to map processed by for release, refund, forfeit
        var transactionIds = items.Items.Select(t => (TransactionId?)TransactionId.From(t.Id)).ToList();
        var escrowTxns = await _dbContext.Set<Domain.Context.PaymentContext.Aggregates.Escrows.Escrow>()
            .AsNoTracking()
            .Include(e => e.ReleaseEvents)
            .Where(e => e.ReleaseTransactionId != null && transactionIds.Contains(e.ReleaseTransactionId))
            .ToListAsync(cancellationToken);

        var escrowByTxnId = escrowTxns.DistinctBy(e => e.ReleaseTransactionId!.Value).ToDictionary(e => e.ReleaseTransactionId!.Value);
        
        foreach (var escrow in escrowTxns)
        {
            var latestReleaseEvent = escrow.ReleaseEvents.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            if (latestReleaseEvent?.CreatedBy != null)
            {
                userIds.Add(latestReleaseEvent.CreatedBy.Value);
            }
        }

        // Re-fetch users if we added new admin ids
        var additionalUserIds = userIds.Except(usersById.Keys.Select(k => UserId.From(k))).ToList();
        if (additionalUserIds.Count > 0)
        {
            var additionalUsers = await _dbContext.Set<User>()
                .AsNoTracking()
                .Where(u => additionalUserIds.Contains(u.Id))
                .ToListAsync(cancellationToken);
            foreach (var au in additionalUsers)
                usersById[au.Id.Value] = au;
        }

        var enriched = items.Items
            .Select(t => 
            {
                var wReq = withdrawRequestsByTxnId.GetValueOrDefault(t.Id);
                var escrow = escrowByTxnId.GetValueOrDefault(TransactionId.From(t.Id));
                var latestEscrowEvent = escrow?.ReleaseEvents.OrderByDescending(x => x.CreatedAt).FirstOrDefault();

                var processedBy = wReq?.ProcessedBy?.Value ?? latestEscrowEvent?.CreatedBy?.Value;
                
                string? processedByName = null;
                if (processedBy.HasValue)
                {
                    if (usersById.TryGetValue(processedBy.Value, out var au))
                    {
                        processedByName = ResolveUserDisplayName(au);
                    }
                }

                var processNote = wReq?.TransferNote ?? wReq?.RejectionReason ?? latestEscrowEvent?.TriggerSourceType;
                
                return t with
                {
                    UserDisplayName = usersById.TryGetValue(t.UserId, out var u)
                        ? ResolveUserDisplayName(u) : null,
                    OrderNumber = t.OrderId.HasValue && orderNumberById.TryGetValue(t.OrderId.Value, out var on)
                        ? on : null,
                    AuctionItemTitle = t.AuctionId.HasValue && titleByAuctionId.TryGetValue(t.AuctionId.Value, out var at)
                        ? at : null,
                    ProcessedBy = processedBy,
                    ProcessedByDisplayName = processedByName,
                    ProcessNote = processNote
                };
            })
            .ToList();

        return new PagedList<PaymentTransactionDto>(
            enriched, items.Metadata.TotalCount,
            items.Metadata.CurrentPage, items.Metadata.PageSize);
    }
}
