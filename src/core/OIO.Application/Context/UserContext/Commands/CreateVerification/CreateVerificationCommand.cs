using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.CreateVerification;

public sealed record CreateVerificationCommand(
    string VerificationType) : ICommand<VerificationDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return CreateVerificationCommand.Check()
            .WithOwnerName("CreateVerification")
            .Field(VerificationType)
            .NotWhiteSpace()
            .InSet(Domain.Context.UserContext.Enums.VerificationType.All.Select(x => x.Id));
    }
}

internal sealed class CreateVerificationCommandHandler
    : ICommandHandler<CreateVerificationCommand, VerificationDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public CreateVerificationCommandHandler(
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

    public async Task<Result<VerificationDto, Error>> Handle(
        CreateVerificationCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var nowUtc = _clock.UtcNow;

        var hasPending = await _dbContext.Set<IdentityVerification>()
            .AnyAsync(v => v.UserId == userId
                           && (v.Status == IdentityVerificationStatus.Pending
                               || v.Status == IdentityVerificationStatus.Submitted
                               || v.Status == IdentityVerificationStatus.UnderReview),
                cancellationToken);

        if (hasPending)
            return UserErrors.Verification.HasPendingVerification;

        var verificationType = VerificationType.FromId(request.VerificationType).Value;

        var verification = IdentityVerification.Create(userId, verificationType, nowUtc);

        _dbContext.Insert(verification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return verification.ToDto();
    }
}
