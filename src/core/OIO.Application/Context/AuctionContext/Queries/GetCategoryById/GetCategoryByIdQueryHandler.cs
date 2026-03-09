using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Domain.Context.AuctionContext.Aggregates.Categories;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetCategoryById;

internal sealed class GetCategoryByIdQueryHandler
    : IQueryHandler<GetCategoryByIdQuery, CategoryDto>
{
    private readonly IDbContext _dbContext;

    public GetCategoryByIdQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CategoryDto, Error>> Handle(
        GetCategoryByIdQuery request,
        CancellationToken cancellationToken)
    {
        var categoryId = CategoryId.From(request.CategoryId);
        
        var category = await _dbContext.GetByIdAsync<Category, CategoryId>(
            id: categoryId,
            queryBuilder: query => query
                .AsNoTracking(),
            cancellationToken: cancellationToken);

        if (category is null)
            return AuctionErrors.Category.NotFound(categoryId);

        return category.ToDto();
    }
}