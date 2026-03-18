using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.AuctionContext.Queries.GetItemReviewHistory;

public sealed record GetItemReviewHistoryQuery(Guid ItemId) : IQuery<IReadOnlyList<ItemModerationReviewDto>>;

internal sealed class GetItemReviewHistoryQueryHandler
    : IQueryHandler<GetItemReviewHistoryQuery, IReadOnlyList<ItemModerationReviewDto>>
{
    private readonly IDbContext _dbContext;

    public GetItemReviewHistoryQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<ItemModerationReviewDto>, Error>> Handle(
        GetItemReviewHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var itemId = ItemId.From(request.ItemId);

        var item = await _dbContext.GetByIdAsync<Item, ItemId>(
            id: itemId,
            queryBuilder: q => q.AsNoTracking()
                .Include(i => i.ModerationReviews),
            cancellationToken: cancellationToken);

        if (item is null)
            return AuctionErrors.Item.NotFound(itemId);

        var reviews = item.ModerationReviews
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ItemModerationReviewDto(
                Id: r.Id.Value,
                Action: r.Action.Id,
                ReviewerId: r.ReviewerId.Value,
                Reason: r.Reason,
                OldStatus: r.OldStatus,
                NewStatus: r.NewStatus,
                CreatedAt: r.CreatedAt))
            .ToList();

        return Result.Success<IReadOnlyList<ItemModerationReviewDto>, Error>(reviews);
    }
}
