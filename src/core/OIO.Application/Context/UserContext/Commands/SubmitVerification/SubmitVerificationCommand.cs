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

namespace OIO.Application.Context.UserContext.Commands.SubmitVerification;

public sealed record SubmitVerificationCommand(Guid VerificationId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return SubmitVerificationCommand.Check()
            .WithOwnerName("SubmitVerification")
            .Field(VerificationId).NotEmptyGuid();
    }
}

internal sealed class SubmitVerificationCommandHandler
    : ICommandHandler<SubmitVerificationCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly VerificationDuplicateIdentityService _duplicateIdentityService;
    private readonly IEnsureTermsAcceptedService _ensureTermsAccepted;

    public SubmitVerificationCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ICurrentUser currentUser,
        VerificationDuplicateIdentityService duplicateIdentityService,
        IEnsureTermsAcceptedService ensureTermsAccepted)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
        _duplicateIdentityService = duplicateIdentityService;
        _ensureTermsAccepted = ensureTermsAccepted;
    }

    public async Task<UnitResult<Error>> Handle(
        SubmitVerificationCommand request,
        CancellationToken cancellationToken)
    {
        // Forced re-acceptance gate: verification submission requires platform terms only.
        // Seller terms are enforced at seller profile creation, not at identity verification.
        var gateResult = await _ensureTermsAccepted.EnsureAsync(
            _currentUser.UserId, ["platform"], cancellationToken);
        if (gateResult.IsFailure)
            return gateResult.Error;

        var userId = _currentUser.UserId;
        var verificationId = IdentityVerificationId.From(request.VerificationId);

        var verification = await _dbContext.GetByIdAsync<IdentityVerification, IdentityVerificationId>(
            verificationId,
            q => q.Include(v => v.Documents),
            cancellationToken);

        if (verification is null || verification.UserId != userId)
            return UserErrors.Verification.NotFound(verificationId);

        var duplicate = await _duplicateIdentityService.FindDuplicateAsync(verification, cancellationToken);
        if (duplicate is not null)
            return UserErrors.Verification.DuplicateIdentity;

        var result = verification.Submit(_clock.UtcNow);

        if (result.IsFailure)
            return result.Error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}
