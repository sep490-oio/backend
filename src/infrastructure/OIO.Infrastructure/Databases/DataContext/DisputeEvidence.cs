using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class DisputeEvidence
{
    public Guid Id { get; set; }

    public Guid DisputeId { get; set; }

    public Guid SubmittedBy { get; set; }

    public string Type { get; set; } = null!;

    public string FileUrl { get; set; } = null!;

    public string? FileName { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Dispute Dispute { get; set; } = null!;

    public virtual User SubmittedByNavigation { get; set; } = null!;
}
