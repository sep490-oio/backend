using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.PaymentContext.Aggregates.PaymentMethods;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Commands.PaymentMethods;

public sealed record AddPaymentMethodCommand(
    string Type,
    string? Provider,
    string? LastFour,
    int? ExpiryMonth,
    int? ExpiryYear,
    string? HolderName,
    string? TokenReference,
    bool IsDefault) : ICommand<Guid>, IHasValidate
{
    public ViolationsError Validate()
    {
        return AddPaymentMethodCommand.Check()
            .WithOwnerName("AddPaymentMethod")
            .Field(Type)
            .NotWhiteSpace()
            .InSet(PaymentMethodType.All.Select(t => t.Id));
    }
}

internal sealed class AddPaymentMethodCommandHandler : ICommandHandler<AddPaymentMethodCommand, Guid>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public AddPaymentMethodCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid, Error>> Handle(AddPaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var typeResult = PaymentMethodType.FromId(request.Type);
        if (typeResult.HasNoValue)
            return Error.Validation("PaymentMethodType", "AddPaymentMethod.InvalidType", "Invalid payment method type.");

        var cardInfo = CardInfo.Create(request.LastFour, request.ExpiryMonth, request.ExpiryYear, request.HolderName);

        // ── Per-type dedupe precheck (UX optimization; DB partial unique indexes are the correctness guarantee).
        // Scope is ALL rows (active + inactive) so the FE can offer a "Reactivate existing" CTA when the
        // collision is with a disabled row. Use direct field comparisons only — no computed properties in Where.
        var userId = _currentUser.UserId;
        var type = typeResult.Value;
        var provider = request.Provider;
        var lastFour = request.LastFour;
        var expiryMonth = request.ExpiryMonth;
        var expiryYear = request.ExpiryYear;
        var holderName = request.HolderName;
        var tokenReference = request.TokenReference;

        PaymentMethod? existing = null;
        if (type == PaymentMethodType.CreditCard || type == PaymentMethodType.DebitCard)
        {
            existing = await _dbContext.Set<PaymentMethod>()
                .FirstOrDefaultAsync(
                    x => x.UserId == userId
                         && x.Type == type
                         && x.Provider == provider
                         && x.Card.LastFour == lastFour
                         && x.Card.ExpiryMonth == expiryMonth
                         && x.Card.ExpiryYear == expiryYear,
                    cancellationToken);
        }
        else if (type == PaymentMethodType.BankAccount)
        {
            existing = await _dbContext.Set<PaymentMethod>()
                .FirstOrDefaultAsync(
                    x => x.UserId == userId
                         && x.Type == type
                         && x.Provider == provider
                         && x.Card.LastFour == lastFour,
                    cancellationToken);
        }
        else if (type == PaymentMethodType.EWallet)
        {
            existing = await _dbContext.Set<PaymentMethod>()
                .FirstOrDefaultAsync(
                    x => x.UserId == userId
                         && x.Type == type
                         && x.Provider == provider
                         && x.Card.HolderName == holderName,
                    cancellationToken);
        }
        // Note: manual Add does not dedupe VnPay. VnPay rows are created only
        // by ProcessVnPayCallback.TryLinkOrCreatePaymentMethodFromTokenAsync,
        // which dedupes on the stable (user, bank_code, masked_card) identity.
        // vnp_token is NOT stable across re-links of the same physical card.

        if (existing is not null)
        {
            return Error.Conflict(
                "PaymentMethod.Duplicate",
                "A payment method with matching details already exists.",
                new Dictionary<string, object>
                {
                    ["conflictingMethodId"] = existing.Id.Value.ToString(),
                    ["existingIsActive"] = existing.IsActive,
                });
        }

        // If this one is set as default, we might need to unset others, but let's handle that in a separate step or here.
        if (request.IsDefault)
        {
            var existingDefaults = await _dbContext.Set<PaymentMethod>()
                .Where(p => p.UserId == _currentUser.UserId && p.IsActive && p.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var p in existingDefaults)
            {
                p.RemoveDefault();
                _dbContext.Update(p);
            }
        }

        var paymentMethod = PaymentMethod.Create(
            userId: _currentUser.UserId,
            type: typeResult.Value,
            provider: request.Provider,
            card: cardInfo,
            tokenReference: request.TokenReference,
            isDefault: request.IsDefault,
            nowUtc: _clock.UtcNow);

        _dbContext.Insert(paymentMethod);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return paymentMethod.Id.Value;
    }
}
