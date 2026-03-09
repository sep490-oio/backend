using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetCategoryBySlug;

public sealed record GetCategoryBySlugQuery(string Slug) : IQuery<CategoryDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetCategoryBySlugQuery.Check()
            .WithOwnerName("GetCategoryBySlug")
            .Field(Slug)
            .NotWhiteSpace();
    }
}