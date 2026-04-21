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

namespace OIO.Application.Context.UserContext.Commands.CreateSellerProfile;

public sealed record CreateSellerProfileCommand(
    string StoreName,
    string StoreDescription) : ICommand<SellerProfileDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return CreateSellerProfileCommand.Check()
            .WithOwnerName("CreateSellerProfile")
            .Field(StoreName).NotWhiteSpace().MaxLength(200)
            .Field(StoreDescription).NotWhiteSpace();
    }
}

internal sealed class CreateSellerProfileCommandHandler
    : ICommandHandler<CreateSellerProfileCommand, SellerProfileDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IEnsureTermsAcceptedService _ensureTermsAccepted;

    public CreateSellerProfileCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ICurrentUser currentUser,
        IEnsureTermsAcceptedService ensureTermsAccepted)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
        _ensureTermsAccepted = ensureTermsAccepted;
    }

    public async Task<Result<SellerProfileDto, Error>> Handle(
        CreateSellerProfileCommand request,
        CancellationToken cancellationToken)
    {
        // Forced re-acceptance gate (plan §3.6.4 / B7): seller profile submission requires both
        // platform and seller terms to be current.
        var gateResult = await _ensureTermsAccepted.EnsureAsync(
            _currentUser.UserId, ["platform", "seller"], cancellationToken);
        if (gateResult.IsFailure)
            return gateResult.Error;

        var userId = _currentUser.UserId;

        var existingProfile = await _dbContext.Set<SellerProfile>()
            .AnyAsync(p => p.Id == userId, cancellationToken);

        if (existingProfile)
            return UserErrors.SellerProfile.AlreadyExists;

        var hasApprovedVerification = await _dbContext.Set<IdentityVerification>()
            .AnyAsync(v => v.UserId == userId
                           && v.Status == IdentityVerificationStatus.Approved,
                cancellationToken);

        if (!hasApprovedVerification)
            return UserErrors.SellerProfile.IdentityNotVerified;

        var profile = SellerProfile.Create(
            userId, 
            request.StoreName,
            request.StoreDescription,
            _clock.UtcNow);

        _dbContext.Insert(profile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return profile.ToDto();
    }
}
