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

namespace OIO.Application.Context.UserContext.Commands.ApproveVerification;

public sealed record ApproveVerificationCommand(Guid VerificationId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return ApproveVerificationCommand.Check()
            .WithOwnerName("ApproveVerification")
            .Field(VerificationId).NotEmptyGuid();
    }
}

internal sealed class ApproveVerificationCommandHandler
    : ICommandHandler<ApproveVerificationCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly VerificationDuplicateIdentityService _duplicateIdentityService;

    public ApproveVerificationCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ICurrentUser currentUser,
        VerificationDuplicateIdentityService duplicateIdentityService)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
        _duplicateIdentityService = duplicateIdentityService;
    }

    public async Task<UnitResult<Error>> Handle(
        ApproveVerificationCommand request,
        CancellationToken cancellationToken)
    {
        var verificationId = IdentityVerificationId.From(request.VerificationId);

        var verification = await _dbContext.GetByIdAsync<IdentityVerification, IdentityVerificationId>(
            verificationId,
            q => q.Include(v => v.Documents),
            cancellationToken);

        if (verification is null)
            return UserErrors.Verification.NotFound(verificationId);

        var duplicate = await _duplicateIdentityService.FindDuplicateAsync(verification, cancellationToken);
        if (duplicate is not null)
            return UserErrors.Verification.DuplicateIdentity;

        var result = verification.Approve(_currentUser.UserId, _clock.UtcNow);

        if (result.IsFailure)
            return result.Error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}
