using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetReviewQueue;

public record  GetReviewQueueQueryFilterParameters : PagedParameters
{
    public string? Status { get; init; }
    public Guid? AssignedAdminId { get; init; }
}

public sealed record GetReviewQueueQuery(
    GetReviewQueueQueryFilterParameters Parameters) : IQuery<PagedList<ReviewQueueItemDto>>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetReviewQueueQuery.Check()
            .WithOwnerName("GetReviewQueue")
            .Field(Parameters.Status)
            .WhenHasValue( x => x.InSet(ItemStatus.All.Select(status => status.Id)))
            .Field(Parameters.AssignedAdminId)
            .WhenHasValue(x => x.NotEmptyGuid());
    }
}

internal sealed class GetReviewQueueQueryHandler
    : IQueryHandler<GetReviewQueueQuery, PagedList<ReviewQueueItemDto>>
{
    private readonly IDbContext _dbContext;

    public GetReviewQueueQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedList<ReviewQueueItemDto>, Error>> Handle(
        GetReviewQueueQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
            
        var query = _dbContext.Set<Item>()
            .AsNoTracking()
            .Where(i => i.Status == ItemStatus.PendingReview);

        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var status = ItemStatus.FromId(parameters.Status).Value;
            query = query.Where(i => i.Status == status);
        }

        if (parameters.AssignedAdminId.HasValue)
        {
            var adminId = UserId.From(parameters.AssignedAdminId.Value);
            query = query.Where(i => i.AssignedAdminId == adminId);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(i => i.SubmittedAt)
            .Select(item => new ReviewQueueItemDto(
                ItemId: item.Id.Value,
                AuctionId: item.Auctions
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => a.Id.Value)
                    .FirstOrDefault(),
                Title: item.Title.Value,
                Status: item.Status.Id,
                Condition: item.Condition.Id,
                SellerId: item.SellerId.Value,
                AssignedAdminId: item.AssignedAdminId != null ? item.AssignedAdminId.Value.Value : null,
                ResubmissionCount: item.ResubmissionCount,
                MediaCount: item.Media.Count,
                SubmittedAt: item.SubmittedAt,
                CreatedAt: item.CreatedAt))
            .ToPagedListAsync(totalCount, parameters, cancellationToken);


        return items;
    }
}
