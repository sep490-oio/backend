using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Auctions.Queries.GetMyAuctionStats;

internal sealed class GetMyAuctionStatsQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyAuctionStatsQuery, SellerAuctionStatsDto>
{
    public async Task<Result<SellerAuctionStatsDto, Error>> Handle(
        GetMyAuctionStatsQuery request,
        CancellationToken ct)
    {
        var sellerId = currentUser.UserId;

        var baseQuery = dbContext.Set<Auction>()
            .AsNoTracking()
            .Where(a => a.Item.SellerId == sellerId);

        var totalCount = await baseQuery.CountAsync(ct);

        var activeCount = await baseQuery
            .Where(a => a.Status.Id == AuctionStatus.Active.Id || a.Status.Id == AuctionStatus.Scheduled.Id)
            .CountAsync(ct);

        var draftCount = await baseQuery
            .Where(a => a.Status.Id == AuctionStatus.Draft.Id)
            .CountAsync(ct);

        var endedCount = await baseQuery
            .Where(a => a.Status.Id == AuctionStatus.Ended.Id || 
                        a.Status.Id == AuctionStatus.Sold.Id || 
                        a.Status.Id == AuctionStatus.Completed.Id || 
                        a.Status.Id == AuctionStatus.Failed.Id || 
                        a.Status.Id == AuctionStatus.Cancelled.Id || 
                        a.Status.Id == AuctionStatus.Terminated.Id || 
                        a.Status.Id == AuctionStatus.PaymentDefaulted.Id)
            .CountAsync(ct);

        return new SellerAuctionStatsDto(totalCount, activeCount, draftCount, endedCount);
    }
}
