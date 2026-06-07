using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Extensions;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Queries.GetMyWalletTransactions;

public record WalletTransactionFilterParameters : PagedParameters
{
    public string? Type { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}

public sealed record GetMyWalletTransactionsQuery(
    WalletTransactionFilterParameters Parameters) : IQuery<PagedList<WalletTransactionDto>>;

internal sealed class GetMyWalletTransactionsQueryHandler
    : IQueryHandler<GetMyWalletTransactionsQuery, PagedList<WalletTransactionDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMyWalletTransactionsQueryHandler(IDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedList<WalletTransactionDto>, Error>> Handle(
        GetMyWalletTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var query = _dbContext.Set<WalletTransaction>()
            .AsNoTracking()
            .Include(x => x.Wallet)
            .Include(x => x.Transaction)
            .Where(x => x.Wallet.UserId == _currentUser.UserId);

        if (!string.IsNullOrWhiteSpace(parameters.Type))
        {
            var type = WalletTransactionType.FromId(parameters.Type);
            if (type.HasNoValue)
                return Error.Validation("type", "WalletTransaction.InvalidType", "Unsupported wallet transaction type.");

            query = query.Where(x => x.Type == type.Value);
        }

        if (parameters.From.HasValue)
            query = query.Where(x => x.CreatedAt >= parameters.From.Value);

        if (parameters.To.HasValue)
            query = query.Where(x => x.CreatedAt <= parameters.To.Value);

        // Newest-first, but break exact-timestamp ties deterministically. An atomic
        // "fund-then-hold" auction deposit writes a Credit (wallet top-up) and a Hold
        // (auction deposit) with the SAME CreatedAt; without a tie-break their relative
        // order is non-deterministic and the Hold could render above the funding Credit.
        // BalanceBefore follows the ledger build-up (the funding Credit starts from the
        // lower running balance), so it deterministically surfaces the top-up above the
        // deposit hold. Id is a final stable tie-break for pagination.
        query = query
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.BalanceBefore)
            .ThenBy(x => x.Id);

        var count = await query.CountAsync(cancellationToken);
        var items = await query
            .Page(parameters)
            .ToListAsync(cancellationToken);

        var dtos = items
            .Select(x => PaymentReadModelMapper.ToDto(x, x.Wallet.WalletFunds.Currency.Id))
            .ToList();

        return dtos.ToPagedList(count, parameters);
    }
}
