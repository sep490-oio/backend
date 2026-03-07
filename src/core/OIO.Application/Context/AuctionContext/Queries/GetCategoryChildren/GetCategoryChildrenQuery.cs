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

namespace OIO.Application.Context.AuctionContext.Queries.GetCategoryChildren;

public sealed record GetCategoryChildrenQuery(Guid CategoryId) : IQuery<IReadOnlyList<CategoryDto>>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetCategoryChildrenQuery.Check()
            .WithOwnerName("GetCategoryChildren")
            .Field(CategoryId)
            .NotEmptyGuid();
    }
}

internal sealed class GetCategoryChildrenQueryHandler
    : IQueryHandler<GetCategoryChildrenQuery, IReadOnlyList<CategoryDto>>
{
    private readonly IDbContext _dbContext;

    public GetCategoryChildrenQueryHandler(
        IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<CategoryDto>, Error>> Handle(
        GetCategoryChildrenQuery request,
        CancellationToken cancellationToken)
    {
        var parentId = CategoryId.From(request.CategoryId);
        var parentExists = await _dbContext.Set<Category>()
            .AnyAsync(x => x.Id == parentId, cancellationToken);

        if (!parentExists)
            return AuctionErrors.Category.NotFound(parentId);

        var children = await _dbContext.Set<Category>()
            .Where(c => c.ParentId == parentId && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => c.ToDto())
            .ToListAsync(cancellationToken);

        return children;
    }
}