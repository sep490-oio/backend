using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class SellerProfile
{
    public Guid Id { get; set; }

    public string StoreName { get; set; } = null!;

    public string StoreDescription { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime? VerifiedAt { get; set; }

    public int TotalSalesCount { get; set; }

    public decimal TotalSalesAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual User IdNavigation { get; set; } = null!;

    public virtual SellerKyc? SellerKyc { get; set; }
}
