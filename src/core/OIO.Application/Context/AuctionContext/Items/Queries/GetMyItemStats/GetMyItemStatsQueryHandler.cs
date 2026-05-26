using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Items.Queries.GetMyItemStats;

internal sealed class GetMyItemStatsQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyItemStatsQuery, SellerItemStatsDto>
{
    public async Task<Result<SellerItemStatsDto, Error>> Handle(
        GetMyItemStatsQuery request,
        CancellationToken ct)
    {
        var sellerId = currentUser.UserId;

        var baseQuery = dbContext.Set<Item>()
            .AsNoTracking()
            .Where(i => i.SellerId == sellerId);

        var totalItems = await baseQuery.CountAsync(ct);

        var pendingReviewItems = await baseQuery
            .Where(i => i.Status.Id == ItemStatus.PendingReview.Id)
            .CountAsync(ct);

        var rejectedItems = await baseQuery
            .Where(i => i.Status.Id == ItemStatus.Rejected.Id)
            .CountAsync(ct);

        return new SellerItemStatsDto(totalItems, pendingReviewItems, rejectedItems);
    }
}
