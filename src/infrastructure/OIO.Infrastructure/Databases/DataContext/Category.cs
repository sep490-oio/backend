using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class Category
{
    public Guid Id { get; set; }

    public Guid? ParentId { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public string? IconUrl { get; set; }

    public bool? IsActive { get; set; }

    public int? SortOrder { get; set; }

    public string? Path { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Category> InverseParent { get; set; } = new List<Category>();

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();

    public virtual Category? Parent { get; set; }
}
