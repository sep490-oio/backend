using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class ItemImage
{
    public Guid Id { get; set; }

    public Guid ItemId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public bool? IsPrimary { get; set; }

    public int? SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Item Item { get; set; } = null!;
}
