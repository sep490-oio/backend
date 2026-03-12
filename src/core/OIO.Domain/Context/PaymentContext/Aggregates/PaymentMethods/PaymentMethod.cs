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

    private PaymentMethod() { }
}