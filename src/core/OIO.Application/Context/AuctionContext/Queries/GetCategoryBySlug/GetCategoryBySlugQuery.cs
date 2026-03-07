using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Domain.Context.AuctionContext.Aggregates.Categories;
using OIO.Domain.Context.AuctionContext.Errors;
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

internal sealed class GetCategoryBySlugQueryHandler
    : IQueryHandler<GetCategoryBySlugQuery, CategoryDto>
{
    private readonly IDbContext _dbContext;

    public GetCategoryBySlugQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CategoryDto, Error>> Handle(
        GetCategoryBySlugQuery request,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        var category = await _dbContext.Set<Category>()
            .FirstOrDefaultAsync(c => c.Slug == normalizedSlug, cancellationToken);
        
        if (category is null)
            return AuctionErrors.Category.NotFoundWithSlug(request.Slug);

        return category.ToDto();
    }
}