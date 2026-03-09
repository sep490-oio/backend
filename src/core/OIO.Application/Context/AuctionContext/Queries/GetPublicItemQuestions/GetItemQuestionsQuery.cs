using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetPublicItemQuestions;

public sealed record GetPublicItemQuestionsQuery(
    Guid ItemId,
    GetPublicItemQuestionsFilterParameters Parameters) : IQuery<PagedList<ItemQuestionDto>>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetPublicItemQuestionsQuery.Check()
            .WithOwnerName("GetPublicItemQuestions")
            .Field(ItemId)
            .NotEmptyGuid()
            .Field(Parameters.SortBy)
            .WhenHasValue(x => x.Must(ItemQuestionMappings.SortMapping.ValidateMappings));
        
    }
}