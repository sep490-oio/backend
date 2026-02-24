using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class ReviewImage
{
    public Guid Id { get; set; }

    public Guid ReviewId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public int? SortOrder { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual SellerReview Review { get; set; } = null!;
}
