using OIO.Application.Abstractions.Sorting;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;

namespace OIO.Application.Context.AuctionContext.Mappings;

public static class ItemQuestionMappings
{
    public static ItemQuestionDto ToDto(this ItemQuestion question)
    {
        return new ItemQuestionDto(
            Id: question.Id.Value,
            AskerId: question.AskerId.Value,
            Question: question.Question,
            Answer: question.Answer,
            AnsweredAt: question.AnsweredAt,
            IsPublic: question.IsPublic,
            CreatedAt: question.CreatedAt);
    }
    
    public static readonly SortMappingDefinition SortMapping = SortMappingBuilder<ItemQuestionDto, ItemQuestion>
        .Create()
        .Map(x => x.Id, x => x.Id)
        .Map(x => x.AskerId, x => x.AskerId)
        .Map(x => x.Question, x => x.Question)
        .Map(x => x.Answer, x => x.Answer)
        .Map(x => x.AnsweredAt, x => x.AnsweredAt)
        .Map(x => x.CreatedAt, x => x.CreatedAt)
        .Build();
}