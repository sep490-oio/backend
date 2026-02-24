using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class BuyerReview
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid ReviewerId { get; set; }

    public Guid BuyerId { get; set; }

    public short OverallRating { get; set; }

    public short? PaymentSpeedRating { get; set; }

    public short? CommunicationRating { get; set; }

    public string? Comment { get; set; }

    public string? Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User Buyer { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual User Reviewer { get; set; } = null!;
}
