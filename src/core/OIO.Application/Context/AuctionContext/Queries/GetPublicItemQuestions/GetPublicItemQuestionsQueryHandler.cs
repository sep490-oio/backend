using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Items;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

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
        
        var itemDtoList = await query 
            .Select(question => new ItemQuestionDto(
                Id: question.Id.Value,
                AskerId: question.AskerId.Value,
                Question: question.Question,
                Answer: question.Answer,
                AnsweredAt: question.AnsweredAt,
                IsPublic: question.IsPublic,
                CreatedAt: question.CreatedAt))
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        return itemDtoList;
    }
}