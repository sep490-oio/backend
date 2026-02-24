using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class ReviewReport
{
    public Guid Id { get; set; }

    public Guid ReviewId { get; set; }

    public Guid ReporterId { get; set; }

    public string Reason { get; set; } = null!;

    public string? Description { get; set; }

    public string? Status { get; set; }

    public Guid? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User Reporter { get; set; } = null!;

    public virtual SellerReview Review { get; set; } = null!;

    public virtual User? ReviewedByNavigation { get; set; }
}
