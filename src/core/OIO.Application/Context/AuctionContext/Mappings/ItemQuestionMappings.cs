using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.Context.AuctionContext.Aggregates.Items;

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
}