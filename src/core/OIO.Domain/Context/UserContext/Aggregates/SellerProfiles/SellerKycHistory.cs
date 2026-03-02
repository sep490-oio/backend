using System.Net;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.UserContext.Aggregates.SellerProfiles;

public sealed class SellerKycHistory : BaseEntity<SellerKycHistoryId>
{
    private SellerKycHistory() {}
    
    public SellerKycId KycId { get; private set; }

    public SellerKycHistoryAction Action { get; private set; }

    public SellerKycStatus? OldStatus { get; private set; }

    public SellerKycStatus? NewStatus { get; private set; }

    public string? ChangedFields { get; private set; }

    public string? Notes { get; private set; }

    public UserId? PerformedBy { get; private set; }

    public KycHistoryPerformedByType? PerformedByType { get; private set; }

    public IPAddress? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public DateTime CreatedAt { get; private set; }
}