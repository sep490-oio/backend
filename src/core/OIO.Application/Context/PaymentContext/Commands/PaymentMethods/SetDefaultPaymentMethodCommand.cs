using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.PaymentContext.Aggregates.PaymentMethods;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Commands.PaymentMethods;

public sealed record SetDefaultPaymentMethodCommand(Guid PaymentMethodId) : ICommand;

internal sealed class SetDefaultPaymentMethodCommandHandler : ICommandHandler<SetDefaultPaymentMethodCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public SetDefaultPaymentMethodCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<UnitResult<Error>> Handle(SetDefaultPaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var targetMethod = await _dbContext.Set<PaymentMethod>()
            .FirstOrDefaultAsync(p => p.Id.Value == request.PaymentMethodId && p.UserId == _currentUser.UserId, cancellationToken);

        if (targetMethod is null)
            return Error.NotFound("PaymentMethod.NotFound", "Payment method not found.");

        if (!targetMethod.IsActive)
            return Error.Validation("PaymentMethod", "PaymentMethod.Inactive", "Cannot set an inactive payment method as default.");

        if (targetMethod.IsDefault)
            return UnitResult.Success<Error>();

        // Remove default from others
        var existingDefaults = await _dbContext.Set<PaymentMethod>()
            .Where(p => p.UserId == _currentUser.UserId && p.IsActive && p.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var p in existingDefaults)
        {
            p.RemoveDefault();
            _dbContext.Update(p);
        }

        targetMethod.SetDefault();
        _dbContext.Update(targetMethod);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}
