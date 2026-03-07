using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Domain.Context.AuctionContext.Aggregates.Items;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetItemQuestions;

public sealed record GetItemQuestionsQuery(Guid ItemId) : IQuery<IReadOnlyList<ItemQuestionDto>>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetItemQuestionsQuery.Check()
            .WithOwnerName("GetItemQuestions")
            .Field(ItemId)
            .NotEmptyGuid();
    }
}

internal sealed class GetItemQuestionsQueryHandler
    : IQueryHandler<GetItemQuestionsQuery, IReadOnlyList<ItemQuestionDto>>
{
    private readonly IDbContext _dbContext;

    public GetItemQuestionsQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<ItemQuestionDto>, Error>> Handle(
        GetItemQuestionsQuery request,
        CancellationToken cancellationToken)
    {
        var itemId = ItemId.From(request.ItemId);
        
        if(!(await _dbContext.Set<Item>().AnyAsync(x => x.Id == itemId, cancellationToken)))
            return AuctionErrors.Item.NotFound(itemId);
        
        var itemDtoList = await _dbContext.Set<ItemQuestion>()
            .Where(q => q.ItemId == itemId)
            .Where(q => q.IsPublic)
            .Select(x => x.ToDto())
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync(cancellationToken);

        return itemDtoList;
    }
}