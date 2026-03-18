using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.Aggregates.Categories;
using OIO.Domain.SeedWork.Errors;
using CategoryId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.CategoryId;

namespace OIO.Application.Context.AuctionContext.Queries.GetCategoryChildren;

internal sealed class GetCategoryChildrenQueryHandler
    : IQueryHandler<GetCategoryChildrenQuery, PagedList<CategoryDto>>
{
    private readonly IDbContext _dbContext;

    public GetCategoryChildrenQueryHandler(
        IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedList<CategoryDto>, Error>> Handle(
        GetCategoryChildrenQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        
        var parentId = CategoryId.From(request.CategoryId);
        
        var query = _dbContext.Set<Category>()
            .AsNoTrackingWithIdentityResolution();
        
        var parentExists = await query
            .AnyAsync(x => x.Id == parentId, cancellationToken);

        if (!parentExists)
            return AuctionErrors.Category.NotFound(parentId);

        query = query
            .Where(c => c.ParentId == parentId && c.IsActive)
            .ApplySort(parameters, CategoryMappings.SortMapping);
        
        var totalCount = await query.CountAsync(cancellationToken);
        
        var pagedChildren = await query
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        var children = pagedChildren.Items
            .Select(c => c.ToDto())
            .ToList();

        return children.ToPagedList(totalCount, parameters);
    }
}
