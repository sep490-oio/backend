using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Extensions;
using OIO.Domain.Context.CatalogContext.Aggregates.Categories;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetAllActiveCategories;

internal sealed class GetAllActiveCategoriesQueryHandler
    : IQueryHandler<GetAllActiveCategoriesQuery, PagedList<CategoryDto>>
{
    private readonly IDbContext _dbContext;

    public GetAllActiveCategoriesQueryHandler(
        IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedList<CategoryDto>, Error>> Handle(
        GetAllActiveCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;

        var query = _dbContext.Set<Category>()
            .AsNoTrackingWithIdentityResolution()
            .Where(c => c.IsActive)
            .ApplySort(parameters, CategoryMappings.SortMapping);
        
        var totalCount = await query.CountAsync(cancellationToken);

        var categories = await query
            .Select(category => new CategoryDto(
                Id: category.Id.Value,
                ParentId: category.ParentId.HasValue ? category.ParentId.Value.Value : null,
                Name: category.Name,
                Slug: category.Slug.Value,
                Description: category.Description,
                IconUrl: category.IconInfo != null ? category.IconInfo.SecureUrl : null,
                IsActive: category.IsActive,
                SortOrder: category.SortOrder,
                Path: category.Path.Value,
                CreatedAt: category.CreatedAt))
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        return categories;
    }
}