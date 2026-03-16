using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.PaymentContext.Aggregates.PaymentMethods;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Commands.PaymentMethods;

public sealed record DeletePaymentMethodCommand(Guid PaymentMethodId) : ICommand;

internal sealed class DeletePaymentMethodCommandHandler : ICommandHandler<DeletePaymentMethodCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeletePaymentMethodCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<UnitResult<Error>> Handle(DeletePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var targetMethod = await _dbContext.Set<PaymentMethod>()
            .FirstOrDefaultAsync(p => p.Id.Value == request.PaymentMethodId && p.UserId == _currentUser.UserId, cancellationToken);

        if (targetMethod is null)
            return Error.NotFound("PaymentMethod.NotFound", "Payment method not found.");

        if (!targetMethod.IsActive)
            return UnitResult.Success<Error>();

        targetMethod.Deactivate();
        _dbContext.Update(targetMethod);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}
