using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetAllActiveCategories;

public sealed record GetAllActiveCategoriesQuery(GetAllActiveCategoriresFilterParameters Parameters) 
    : IQuery<PagedList<CategoryDto>>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetAllActiveCategoriesQuery.Check()
            .WithOwnerName("GetAllActiveCategories")
            .Field(Parameters.SortBy)
            .WhenHasValue(x => x.Must(CategoryMappings.SortMapping.ValidateMappings));
    }
}