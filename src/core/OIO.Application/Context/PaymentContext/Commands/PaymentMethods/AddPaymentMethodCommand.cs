using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.PaymentContext.Aggregates.PaymentMethods;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects;
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
    bool IsDefault) : ICommand<Guid>;

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
