using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Categories;

public sealed class Category : AggregateRoot<CategoryId>, IAuditableEntity
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Slug { get; private set; }
    public bool IsActive { get; private set; }
    
    // Audit
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    private Category() { }

    public static Category Create(string name, string description, string slug)
    {
        return new Category
        {
            Id = CategoryId.From(Guid.CreateVersion7()),
            Name = name,
            Description = description,
            Slug = slug,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string description)
    {
        Name = name;
        Description = description;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Deactivate() => IsActive = false;
}