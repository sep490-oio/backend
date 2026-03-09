using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetCategoryById;

public sealed record GetCategoryByIdQuery(Guid CategoryId) : IQuery<CategoryDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetCategoryByIdQuery.Check()
            .WithOwnerName("GetCategoryById")
            .Field(CategoryId)
            .NotEmptyGuid();
    }
}