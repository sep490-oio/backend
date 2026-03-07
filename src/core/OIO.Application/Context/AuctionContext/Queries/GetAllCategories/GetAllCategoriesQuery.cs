using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Domain.Context.AuctionContext.Aggregates.Categories;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetAllCategories;

public sealed record GetAllCategoriesQuery : IQuery<IReadOnlyList<CategoryDto>>;

internal sealed class GetAllCategoriesQueryHandler
    : IQueryHandler<GetAllCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    private readonly IDbContext _dbContext;

    public GetAllCategoriesQueryHandler(
        IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<CategoryDto>, Error>> Handle(
        GetAllCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await _dbContext.Set<Category>()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => c.ToDto())
            .ToListAsync(cancellationToken);

        return categories;
    }
}