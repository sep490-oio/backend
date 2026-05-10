using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyDeposits;

public sealed record MyDepositDto(
    Guid DepositId,
    Guid AuctionId,
    Guid ItemId,
    string ItemTitle,
    string? PrimaryImageUrl,
    string AuctionStatus,
    MoneyDto? CurrentPrice,
    decimal DepositAmount,
    string DepositCurrency,
    string DepositStatus,
    DateTime DepositedAt,
    DateTime? ReleasedAt,
    string? AuctionEndTime,
    string? AuctionStartTime);

public record GetMyDepositsFilterParameters : PagedParameters, ISortByParameter
{
    public string? Status { get; init; }
    public string? SortBy { get; init; }
}

public sealed record GetMyDepositsQuery(
    GetMyDepositsFilterParameters Parameters) : IQuery<PagedList<MyDepositDto>>;

internal sealed class GetMyDepositsQueryHandler
    : IQueryHandler<GetMyDepositsQuery, PagedList<MyDepositDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMyDepositsQueryHandler(IDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    private static readonly Dictionary<string, string> SortMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        ["DepositedAt"] = nameof(AuctionDeposit.CreatedAt),
        ["Amount"] = "Amount.Amount",
    };

    public async Task<Result<PagedList<MyDepositDto>, Error>> Handle(
        GetMyDepositsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var userId = _currentUser.UserId;

        var query = _dbContext.Set<AuctionDeposit>()
            .AsNoTracking()
            .Where(d => d.BidderId == userId);

        // Filter by deposit status
        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            query = parameters.Status.ToLowerInvariant() switch
            {
                "held" => query.Where(d => d.Status == Domain.Context.AuctionContext.Enums.DepositStatus.Held),
                "returned" => query.Where(d => d.Status == Domain.Context.AuctionContext.Enums.DepositStatus.Returned),
                "forfeited" => query.Where(d => d.Status == Domain.Context.AuctionContext.Enums.DepositStatus.Forfeited),
                "converted_to_payment" => query.Where(d => d.Status == Domain.Context.AuctionContext.Enums.DepositStatus.ConvertedToPayment),
                _ => query,
            };
        }

        // Default sort: newest first
        query = query.OrderByDescending(d => d.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var deposits = await query
            .Select(d => new MyDepositDto(
                d.Id.Value,
                d.AuctionId.Value,
                d.Auction.Item.Id.Value,
                d.Auction.Item.Title.Value,
                d.Auction.Item.Media
                    .Where(img => img.IsPrimary)
                    .Select(img => img.Info.SecureUrl)
                    .FirstOrDefault(),
                d.Auction.Status.Id,
                new MoneyDto(d.Auction.Pricing.CurrentPrice.Amount, d.Auction.Pricing.CurrentPrice.Currency.Id, d.Auction.Pricing.CurrentPrice.Currency.Symbol),
                d.Amount.Amount,
                d.Amount.Currency.Id,
                d.Status.Id,
                d.CreatedAt,
                d.ReleasedAt,
                d.Auction.Info != null && d.Auction.Info.EndTime != default
                    ? d.Auction.Info.EndTime.ToString("O")
                    : null,
                d.Auction.Info != null && d.Auction.Info.StartTime != default
                    ? d.Auction.Info.StartTime.ToString("O")
                    : null))
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        return deposits;
    }
}
