using OIO.Domain.Context.AuctionContext.ValueObjects;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.ValueObjects;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Entities;
using CategoryId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.CategoryId;

namespace OIO.Domain.Context.CatalogContext.Aggregates.Categories;

public sealed class Category : AggregateRoot<CategoryId>, IAuditableEntity
{
    private readonly List<Category> _children = [];
    private readonly List<Item> _items = [];
    
    public CategoryId? ParentId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Slug Slug { get; private set; }
    public StorageRef? IconStorageRef { get; private set; }
    public MediaInfo? IconInfo { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }
    public CategoryPath Path { get; private set; }

    // Audit
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }
    
    //Navigation
    public Category? Parent { get; private set; }
    public IReadOnlyCollection<Category> Children => _children.AsReadOnly();
    public IReadOnlyCollection<Item> Items => _items.AsReadOnly();

    private Category() { }

    public static Category Create(
        string name,
        Slug slug,
        DateTime nowUtc,
        CategoryPath? parentPath = null,
        CategoryId? parentId = null,
        string? description = null,
        int sortOrder = 0)
    {
        return new Category
        {
            Id = CategoryId.From(Guid.CreateVersion7()),
            Name = name.Trim(),
            Slug = slug,
            Description = description ?? string.Empty,
            ParentId = parentId,
            SortOrder = sortOrder,
            Path = CategoryPath.FromParent(parentPath, slug),
            IsActive = true,
            CreatedAt = nowUtc
        };
    }
    
    public void Update(
        DateTime nowUtc,
        string? name = null,
        Slug? slug = null,
        string? description = null, 
        MediaInfo? iconInfo = null,
        StorageRef? iconStorage = null,
        bool? isActive = null,
        int? sortOrder = null)
    {
        Name = name ?? Name;
        Slug = slug ?? Slug;
        Description = description ?? Description;
        IconInfo = iconInfo ?? IconInfo;
        IconStorageRef = iconStorage ?? IconStorageRef;
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

    public void RefreshIconMedia(StorageRef storageRef, MediaInfo info, DateTime nowUtc)
    {
        IconStorageRef = storageRef;
        IconInfo = info;
        ModifiedAt = nowUtc;
    }
}
