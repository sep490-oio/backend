using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.UserContext.Aggregates.SellerProfiles;

public sealed class SellerProfile : AggregateRoot<SellerProfileId>, IAuditableEntity
{
    private SellerProfile() {}
    
    public string StoreName { get; private set; }

    public string StoreDescription { get; private set; }

    public SellerProfileStatus Status { get; private set; }

    public DateTime? VerifiedAt { get; private set; }

    public int TotalSalesCount { get; private set; }

    public decimal TotalSalesAmount { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? ModifiedAt { get; private set; }
}