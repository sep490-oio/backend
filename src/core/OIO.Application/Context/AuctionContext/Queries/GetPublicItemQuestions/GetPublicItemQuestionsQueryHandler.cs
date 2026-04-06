using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.EventHandlers;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.AuctionContext.Queries.GetPublicItemQuestions;

internal sealed class GetPublicItemQuestionsQueryHandler
    : IQueryHandler<GetPublicItemQuestionsQuery, PagedList<ItemQuestionDto>>
{
    private readonly IDbContext _dbContext;

    public GetPublicItemQuestionsQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedList<ItemQuestionDto>, Error>> Handle(
        GetPublicItemQuestionsQuery request,
        CancellationToken cancellationToken)
    {
        var itemId = ItemId.From(request.ItemId);
        
        var itemExists = await _dbContext.Set<Item>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == itemId, cancellationToken);
        
        if(!itemExists)
            return AuctionErrors.Item.NotFound(itemId);
        
        var parameters = request.Parameters;

        var query = _dbContext.Set<ItemQuestion>()
            .Where(q => q.ItemId == itemId)
            .Where(q => q.IsPublic)
            .ApplySort(parameters, ItemQuestionMappings.SortMapping, nameof(Item.CreatedAt));

        var totalCount = await query.CountAsync(cancellationToken);

        var questions = await query
            .Select(question => new
            {
                question.Id,
                AskerId = question.AskerId.Value,
                question.Question,
                question.Answer,
                question.AnsweredAt,
                question.IsPublic,
                question.CreatedAt,
            })
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        // Resolve display names for askers
        var askerIds = questions.Items
            .Select(q => UserId.From(q.AskerId))
            .Distinct()
            .ToList();

        // Resolve the seller (answerer) from the item
        var item = await _dbContext.Set<Item>()
            .AsNoTracking()
            .Where(i => i.Id == itemId)
            .Select(i => new { SellerId = i.SellerId.Value })
            .FirstOrDefaultAsync(cancellationToken);

        var sellerUserId = item is not null ? UserId.From(item.SellerId) : (UserId?)null;

        var userIdsToFetch = askerIds.ToHashSet();
        if (sellerUserId.HasValue)
            userIdsToFetch.Add(sellerUserId.Value);

        var users = await _dbContext.Set<User>()
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

        var itemDtoList = new PagedList<ItemQuestionDto>(
            questions.Items.Select(q => new ItemQuestionDto(
                Id: q.Id.Value,
                AskerId: q.AskerId,
                Question: q.Question,
                Answer: q.Answer,
                AnsweredAt: q.AnsweredAt,
                IsPublic: q.IsPublic,
                CreatedAt: q.CreatedAt,
                AskerDisplayName: displayNames.TryGetValue(q.AskerId, out var askerName) ? askerName : null,
                AnswererDisplayName: q.Answer is not null ? answererDisplayName : null))
            .ToList(),
            questions.Metadata);

        return itemDtoList;
    }
}