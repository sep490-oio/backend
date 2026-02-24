using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class PaymentMethod
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Type { get; set; } = null!;

    public string? Provider { get; set; }

    public string? LastFour { get; set; }

    public int? ExpiryMonth { get; set; }

    public int? ExpiryYear { get; set; }

    public string? HolderName { get; set; }

    public bool? IsDefault { get; set; }

    public bool? IsVerified { get; set; }

    public bool? IsActive { get; set; }

    public string? TokenReference { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public virtual User User { get; set; } = null!;
}
