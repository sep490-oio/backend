using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class ItemQuestion
{
    public Guid Id { get; set; }

    public Guid ItemId { get; set; }

    public Guid AskerId { get; set; }

    public string Question { get; set; } = null!;

    public string? Answer { get; set; }

    public DateTime? AnsweredAt { get; set; }

    public bool? IsPublic { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User Asker { get; set; } = null!;

    public virtual Item Item { get; set; } = null!;
}
