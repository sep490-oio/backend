using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.PaymentContext.Aggregates.PaymentMethods;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Commands.PaymentMethods;

public sealed record ReactivatePaymentMethodCommand(Guid PaymentMethodId) : ICommand;

internal sealed class ReactivatePaymentMethodCommandHandler : ICommandHandler<ReactivatePaymentMethodCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public ReactivatePaymentMethodCommandHandler(
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

    public async Task<UnitResult<Error>> Handle(ReactivatePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var paymentMethodId = PaymentMethodId.From(request.PaymentMethodId);

        var target = await _dbContext.Set<PaymentMethod>()
            .FirstOrDefaultAsync(p => p.Id == paymentMethodId && p.UserId == _currentUser.UserId, cancellationToken);

        if (target is null)
            return Error.NotFound("PaymentMethod.NotFound", "Payment method not found.");

        // Domain-layer contract: PaymentMethod.Reactivate() returns
        // Error.Conflict("PaymentMethod.InvalidState") when already active.
        // We rely on the domain method for the idempotency check below.

        // Duplicate-identity precheck (defense-in-depth ahead of DB partial unique index).
        // Scope to active rows excluding the target itself. Direct field comparisons only.
        var userId = _currentUser.UserId;
        var type = target.Type;
        var provider = target.Provider;
        var lastFour = target.Card.LastFour;
        var expiryMonth = target.Card.ExpiryMonth;
        var expiryYear = target.Card.ExpiryYear;
        var holderName = target.Card.HolderName;
        var vnPayToken = target.VnPayToken;
        var targetId = target.Id;

        PaymentMethod? collision = null;
        if (type == PaymentMethodType.CreditCard || type == PaymentMethodType.DebitCard)
        {
            collision = await _dbContext.Set<PaymentMethod>()
                .FirstOrDefaultAsync(
                    x => x.IsActive
                         && x.Id != targetId
                         && x.UserId == userId
                         && x.Type == type
                         && x.Provider == provider
                         && x.Card.LastFour == lastFour
                         && x.Card.ExpiryMonth == expiryMonth
                         && x.Card.ExpiryYear == expiryYear,
                    cancellationToken);
        }
        else if (type == PaymentMethodType.BankAccount)
        {
            collision = await _dbContext.Set<PaymentMethod>()
                .FirstOrDefaultAsync(
                    x => x.IsActive
                         && x.Id != targetId
                         && x.UserId == userId
                         && x.Type == type
                         && x.Provider == provider
                         && x.Card.LastFour == lastFour,
                    cancellationToken);
        }
        else if (type == PaymentMethodType.EWallet)
        {
            collision = await _dbContext.Set<PaymentMethod>()
                .FirstOrDefaultAsync(
                    x => x.IsActive
                         && x.Id != targetId
                         && x.UserId == userId
                         && x.Type == type
                         && x.Provider == provider
                         && x.Card.HolderName == holderName,
                    cancellationToken);
        }
        else if (type == PaymentMethodType.VnPay && !string.IsNullOrWhiteSpace(vnPayToken))
        {
            collision = await _dbContext.Set<PaymentMethod>()
                .FirstOrDefaultAsync(
                    x => x.IsActive
                         && x.Id != targetId
                         && x.UserId == userId
                         && x.VnPayToken == vnPayToken,
                    cancellationToken);
        }

        if (collision is not null)
            return Error.Conflict(
                "PaymentMethod.DuplicateActive",
                "Another active payment method already occupies this identity slot.");

        var reactivateResult = target.Reactivate(_clock.UtcNow);
        if (reactivateResult.IsFailure)
            return reactivateResult.Error;

        _dbContext.Set<PaymentMethod>().Update(target);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}
