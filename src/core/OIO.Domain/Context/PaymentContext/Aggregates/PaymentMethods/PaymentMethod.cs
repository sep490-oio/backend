using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.PaymentContext.Aggregates.PaymentMethods;

public sealed class PaymentMethod : AggregateRoot<PaymentMethodId>, ICreatedAtEntity
{
    public UserId UserId { get; private set; }
    public PaymentMethodType Type { get; private set; }
    public string? Provider { get; private set; }
    public CardInfo Card { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsVerified { get; private set; }
    public bool IsActive { get; private set; }
    public string? TokenReference { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Constructor cho EF Core
    private PaymentMethod() { }

    public static PaymentMethod Create(
        UserId userId,
        PaymentMethodType type,
        string? provider,
        CardInfo card,
        string? tokenReference,
        bool isDefault,
        DateTime nowUtc)
    {
        return new PaymentMethod
        {
            Id = PaymentMethodId.From(Guid.CreateVersion7()),
            UserId = userId,
            Type = type,
            Provider = provider,
            Card = card,
            IsDefault = isDefault,
            IsVerified = true, // Tạm thời mặc định true khi add từ token
            IsActive = true,
            TokenReference = tokenReference,
            CreatedAt = nowUtc
        };
    }

    public void SetDefault()
    {
        IsDefault = true;
    }

    public void RemoveDefault()
    {
        IsDefault = false;
    }

    public void Deactivate()
    {
        IsActive = false;
        IsDefault = false; // Bỏ default khi vô hiệu hóa
    }
}