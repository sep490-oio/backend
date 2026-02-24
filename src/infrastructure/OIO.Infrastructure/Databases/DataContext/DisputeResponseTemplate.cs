using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class DisputeResponseTemplate
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Category { get; set; }

    public string? Subject { get; set; }

    public string Body { get; set; } = null!;

    public bool? IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
