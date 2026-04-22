using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.OrderContext.Aggregates.Orders;

/// <summary>
/// A photo or document linked to an <see cref="OrderReturn"/>.
/// Mirrors <c>OutboundShipmentEvidence</c> — category distinguishes buyer-pickup
/// vs seller-receipt photos.
/// </summary>
public sealed class OrderReturnEvidence : BaseEntity<OrderReturnEvidenceId>, ICreatedAtEntity
{
    private OrderReturnEvidence() { }

    internal OrderReturnEvidence(
        OrderReturnEvidenceId id,
        OrderReturnId orderReturnId,
        string category,
        MediaUploadId mediaUploadId,
        string? secureUrl,
        string? fileName,
        string resourceType,
        DateTime createdAt,
        UserId createdBy)
    {
        Id             = id;
        OrderReturnId  = orderReturnId;
        Category       = category;
        MediaUploadId  = mediaUploadId;
        SecureUrl      = secureUrl;
        FileName       = fileName;
        ResourceType   = resourceType;
        CreatedAt      = createdAt;
        CreatedBy      = createdBy;
    }

    public OrderReturnId OrderReturnId { get; private set; }
    public string Category { get; private set; } = null!;
    public MediaUploadId MediaUploadId { get; private set; }
    public string? SecureUrl { get; private set; }
    public string? FileName { get; private set; }
    public string ResourceType { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public UserId CreatedBy { get; private set; }
}
