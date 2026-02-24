using System;
using System.Collections.Generic;
using System.Net;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class SellerKycHistory
{
    public Guid Id { get; set; }

    public Guid KycId { get; set; }

    public string Action { get; set; } = null!;

    public string? OldStatus { get; set; }

    public string? NewStatus { get; set; }

    public string? ChangedFields { get; set; }

    public string? Notes { get; set; }

    public Guid? PerformedBy { get; set; }

    public string? PerformedByType { get; set; }

    public IPAddress? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual SellerKyc Kyc { get; set; } = null!;

    public virtual User? PerformedByNavigation { get; set; }
}
