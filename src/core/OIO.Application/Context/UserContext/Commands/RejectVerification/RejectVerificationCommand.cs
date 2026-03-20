using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.RejectVerification;

public sealed record RejectVerificationCommand(
    Guid VerificationId,
    string Reason,
    string? RejectionCode = null) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return RejectVerificationCommand.Check()
            .WithOwnerName("RejectVerification")
            .Field(VerificationId).NotEmptyGuid()
            .Field(Reason).NotWhiteSpace().MaxLength(1000)
            .Field(RejectionCode).WhenHasValue(x => x.NotWhiteSpace().MaxLength(50));
    }
}

internal sealed class RejectVerificationCommandHandler
    : ICommandHandler<RejectVerificationCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public RejectVerificationCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<UnitResult<Error>> Handle(
        RejectVerificationCommand request,
        CancellationToken cancellationToken)
    {
        var verificationId = IdentityVerificationId.From(request.VerificationId);

        var verification = await _dbContext.GetByIdAsync<IdentityVerification, IdentityVerificationId>(
            verificationId,
            q => q.Include(v => v.Documents),
            cancellationToken);

        if (verification is null)
            return UserErrors.Verification.NotFound(verificationId);

        var result = verification.Reject(
            _currentUser.UserId, request.Reason, _clock.UtcNow, request.RejectionCode);

        if (result.IsFailure)
            return result.Error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}
