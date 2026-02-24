using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class ReviewVote
{
    public Guid Id { get; set; }

    public Guid ReviewId { get; set; }

    public Guid UserId { get; set; }

    public bool IsHelpful { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual SellerReview Review { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
