using CSharpFunctionalExtensions;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

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

    // ── VNPay Token fields ──────────────────────────────────────────────────
    public string? VnPayToken { get; private set; }
    public string? MaskedCardNumber { get; private set; }
    public string? VnPayCardType { get; private set; }
    public string? BankCode { get; private set; }

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
            IsVerified = true,
            IsActive = true,
            TokenReference = tokenReference,
            CreatedAt = nowUtc
        };
    }

    /// <summary>
    /// Tạo PaymentMethod từ VNPay token callback (pay_and_create hoặc token_create).
    /// </summary>
    public static PaymentMethod CreateFromVnPayToken(
        UserId userId,
        string vnPayToken,
        string? maskedCardNumber,
        string? vnPayCardType,
        string? bankCode,
        bool isDefault,
        DateTime nowUtc)
    {
        var lastFour = maskedCardNumber is { Length: >= 4 }
            ? maskedCardNumber[^4..]
            : null;

        return new PaymentMethod
        {
            Id = PaymentMethodId.From(Guid.CreateVersion7()),
            UserId = userId,
            Type = PaymentMethodType.VnPay,
            Provider = "vnpay",
            Card = CardInfo.Create(lastFour, null, null, null),
            IsDefault = isDefault,
            IsVerified = true,
            IsActive = true,
            TokenReference = vnPayToken,
            VnPayToken = vnPayToken,
            MaskedCardNumber = maskedCardNumber,
            VnPayCardType = vnPayCardType,
            BankCode = bankCode,
            CreatedAt = nowUtc
        };
    }

    /// <summary>
    /// Cập nhật thông tin VNPay token (khi callback trả về data mới).
    /// </summary>
    public void UpdateVnPayToken(string newToken, string? maskedCard, string? cardType, string? bankCode)
    {
        VnPayToken = newToken;
        TokenReference = newToken;
        if (maskedCard is not null) MaskedCardNumber = maskedCard;
        if (cardType is not null) VnPayCardType = cardType;
        if (bankCode is not null) BankCode = bankCode;

        if (maskedCard is { Length: >= 4 })
            Card = CardInfo.Create(maskedCard[^4..], null, null, null);
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
        IsDefault = false;
    }

    /// <summary>
    /// Re-enable a previously deactivated payment method. Does NOT auto-restore default status —
    /// reactivation is an identity-slot claim only; default status must be set explicitly afterwards.
    /// </summary>
    /// <param name="nowUtc">Current UTC timestamp (audit hook — unused today, retained for parity with other lifecycle methods).</param>
    public UnitResult<Error> Reactivate(DateTime nowUtc)
    {
        if (IsActive)
            return Error.Conflict("PaymentMethod.InvalidState", "Payment method is already active");

        IsActive = true;
        _ = nowUtc; // no UpdatedAt field on the aggregate today; kept in signature for audit parity
        return UnitResult.Success<Error>();
    }
}