using OIO.Domain.Context.AuctionContext.ValueObjects;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Categories;

public sealed class Category : AggregateRoot<CategoryId>, IAuditableEntity
{
    public CategoryId? ParentId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Slug { get; private set; }
    public string? IconUrl { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }
    public CategoryPath Path { get; private set; }

    // Audit
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    private Category() { }

    public static Category Create(
        string name,
        string slug,
        DateTime nowUtc,
        CategoryPath? parentPath = null,
        CategoryId? parentId = null,
        string? description = null,
        string? iconUrl = null,
        int sortOrder = 0)
    {
        return new Category
        {
            Id = CategoryId.From(Guid.CreateVersion7()),
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Description = description ?? string.Empty,
            ParentId = parentId,
            IconUrl = iconUrl,
            SortOrder = sortOrder,
            Path = CategoryPath.FromParent(parentPath, slug).Value,
            IsActive = true,
            CreatedAt = nowUtc
        };
    }
    
    public void Update(
        DateTime nowUtc,
        string? name,
        string? slug,
        string? description, 
        string? iconUrl = null,
        bool? isActive = null,
        int? sortOrder = null)
    {
        Name = name ?? Name;
        Slug = slug ?? Slug;
        Description = description ?? Description;
        IconUrl = iconUrl ?? IconUrl;
        SortOrder = sortOrder ?? SortOrder;
        IsActive = isActive ?? IsActive;
        ModifiedAt = nowUtc;
    }

    public void SetParent(CategoryId? parentId, CategoryPath path, DateTime nowUtc)
    {
        ParentId = parentId;
        Path = path;
        ModifiedAt = nowUtc;
    }

    public void Deactivate(DateTime nowUtc)
    {
        IsActive = false;
        ModifiedAt = nowUtc;
    }

    public void Activate(DateTime nowUtc)
    {
        IsActive = true;
        ModifiedAt = nowUtc;
    }
}