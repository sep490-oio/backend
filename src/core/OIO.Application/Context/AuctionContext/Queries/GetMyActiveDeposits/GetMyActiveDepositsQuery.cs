using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyActiveDeposits;

public sealed record ActiveDepositDto(
    Guid DepositId,
    Guid AuctionId,
    string? AuctionTitle,
    decimal Amount,
    string Currency,
    string Status,
    DateTime CreatedAt);

public sealed record GetMyActiveDepositsQuery() : IQuery<IReadOnlyList<ActiveDepositDto>>;

internal sealed class GetMyActiveDepositsQueryHandler
    : IQueryHandler<GetMyActiveDepositsQuery, IReadOnlyList<ActiveDepositDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMyActiveDepositsQueryHandler(IDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<ActiveDepositDto>, Error>> Handle(
        GetMyActiveDepositsQuery request,
        CancellationToken cancellationToken)
    {
        var deposits = await _dbContext.Set<AuctionDeposit>()
            .AsNoTracking()
            .Where(d => d.BidderId == _currentUser.UserId && d.Status == DepositStatus.Held)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new ActiveDepositDto(
                d.Id.Value,
                d.AuctionId.Value,
                d.Auction.Item.Title.Value,
                d.Amount.Amount,
                d.Amount.Currency.Id,
                d.Status.Id,
                d.CreatedAt))
            .ToListAsync(cancellationToken);

        return deposits;
    }
}
