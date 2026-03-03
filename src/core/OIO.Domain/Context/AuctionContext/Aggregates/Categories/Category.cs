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
    public string? Path { get; private set; }

    // Audit
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    private Category() { }

    public static Category Create(
        string name,
        string description,
        string slug,
        CategoryId? parentId = null,
        string? iconUrl = null,
        int sortOrder = 0)
    {
        return new Category
        {
            Id = CategoryId.From(Guid.CreateVersion7()),
            Name = name,
            Description = description,
            Slug = slug,
            ParentId = parentId,
            IconUrl = iconUrl,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string description, string? iconUrl = null, int? sortOrder = null)
    {
        Name = name;
        Description = description;
        IconUrl = iconUrl ?? IconUrl;
        SortOrder = sortOrder ?? SortOrder;
        ModifiedAt = DateTime.UtcNow;
    }

    public void SetParent(CategoryId? parentId, string? path)
    {
        ParentId = parentId;
        Path = path;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        ModifiedAt = DateTime.UtcNow;
    }
}