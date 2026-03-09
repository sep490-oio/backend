using System.Linq.Expressions;
using OIO.Application.Abstractions.Sorting;
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

    public static readonly SortMappingDefinition SortMapping = SortMappingBuilder<CategoryDto, Category>
        .Create()
        .Map(x => x.Id, b => b.Id)
        .Map(x => x.ParentId, b => b.ParentId)
        .Map(x => x.Name, b => b.Name)
        .Map(x => x.Slug, b => b.Slug)
        .Map(x => x.Description, b => b.Description)
        .Map(x => x.IconUrl, b => b.IconUrl)
        .Map(x => x.IsActive, b => b.IsActive)
        .Map(x => x.SortOrder, b => b.SortOrder)
        .Map(x => x.Path, b => b.Path)
        .Map(x => x.CreatedAt, b => b.CreatedAt)
        .Build();
}