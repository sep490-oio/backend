using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.EventHandlers;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.AuctionContext.Queries.GetAdminItemQuestions;

public sealed record GetAdminItemQuestionsQuery(
    Guid ItemId,
    GetAdminItemQuestionsFilterParameters Parameters) : IQuery<PagedList<AdminItemQuestionDto>>;

public record GetAdminItemQuestionsFilterParameters : PagedParameters, ISortByParameter
{
    public string? SortBy { get; init; }
}

internal sealed class GetAdminItemQuestionsQueryHandler(IDbContext dbContext)
    : IQueryHandler<GetAdminItemQuestionsQuery, PagedList<AdminItemQuestionDto>>
{
    public async Task<Result<PagedList<AdminItemQuestionDto>, Error>> Handle(
        GetAdminItemQuestionsQuery request,
        CancellationToken cancellationToken)
    {
        var itemId = ItemId.From(request.ItemId);

        var itemExists = await dbContext.Set<Item>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == itemId, cancellationToken);

        if (!itemExists)
            return AuctionErrors.Item.NotFound(itemId);

        var parameters = request.Parameters;

        // Admin sees ALL questions, including hidden ones — no IsPublic filter
        var query = dbContext.Set<ItemQuestion>()
            .Where(q => q.ItemId == itemId)
            .OrderByDescending(q => q.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var questions = await query
            .Select(q => new
            {
                q.Id,
                AskerId = q.AskerId.Value,
                q.Question,
                q.Answer,
                q.AnsweredAt,
                q.IsPublic,
                q.CreatedAt,
                HiddenByAdminId = q.HiddenByAdminId != null ? (Guid?)q.HiddenByAdminId.Value.Value : null,
                q.HiddenAt,
                q.HiddenReason,
            })
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        // Resolve display names
        var askerIds = questions.Items
            .Select(q => UserId.From(q.AskerId))
            .Distinct()
            .ToList();

        var item = await dbContext.Set<Item>()
            .AsNoTracking()
            .Where(i => i.Id == itemId)
            .Select(i => new { SellerId = i.SellerId.Value })
            .FirstOrDefaultAsync(cancellationToken);

        var sellerUserId = item is not null ? UserId.From(item.SellerId) : (UserId?)null;

        var userIdsToFetch = askerIds.ToHashSet();
        if (sellerUserId.HasValue)
            userIdsToFetch.Add(sellerUserId.Value);

        var users = await dbContext.Set<User>()
            .AsNoTracking()
            .Include(u => u.Profile)
            .Where(u => userIdsToFetch.Contains(u.Id))
            .ToListAsync(cancellationToken);

        var displayNames = users.ToDictionary(
            u => u.Id.Value,
            u => AuctionNotificationDisplayNames.Resolve(u));

        var answererDisplayName = sellerUserId.HasValue && displayNames.TryGetValue(sellerUserId.Value.Value, out var sellerName)
            ? sellerName
            : null;

        var dtoList = new PagedList<AdminItemQuestionDto>(
            questions.Items.Select(q => new AdminItemQuestionDto(
                Id: q.Id.Value,
                AskerId: q.AskerId,
                Question: q.Question,
                Answer: q.Answer,
                AnsweredAt: q.AnsweredAt,
                IsPublic: q.IsPublic,
                CreatedAt: q.CreatedAt,
                AskerDisplayName: displayNames.TryGetValue(q.AskerId, out var askerName) ? askerName : null,
                AnswererDisplayName: q.Answer is not null ? answererDisplayName : null,
                HiddenByAdminId: q.HiddenByAdminId,
                HiddenAt: q.HiddenAt,
                HiddenReason: q.HiddenReason))
            .ToList(),
            questions.Metadata);

        return dtoList;
    }
}
