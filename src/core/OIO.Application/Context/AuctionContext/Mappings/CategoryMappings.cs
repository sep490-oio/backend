using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.Context.AuctionContext.Aggregates.Categories;

namespace OIO.Application.Context.AuctionContext.Mappings;

public static class CategoryMappings
{
    public static CategoryDto ToDto(this Category category)
    {
        return new CategoryDto(
            Id: category.Id.Value,
            ParentId: category.ParentId?.Value,
            Name: category.Name,
            Slug: category.Slug,
            Description: category.Description,
            IconUrl: category.IconUrl,
            IsActive: category.IsActive,
            SortOrder: category.SortOrder,
            Path: category.Path.Value,
            CreatedAt: category.CreatedAt);
    }
}