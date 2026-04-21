using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.UpdateSellerProfile;

public sealed record UpdateSellerProfileCommand(
    string StoreName,
    string StoreDescription) : ICommand<SellerProfileDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return UpdateSellerProfileCommand.Check()
            .WithOwnerName("UpdateSellerProfile")
            .Field(StoreName).NotWhiteSpace().MaxLength(200)
            .Field(StoreDescription).NotWhiteSpace();
    }
}

internal sealed class UpdateSellerProfileCommandHandler
    : ICommandHandler<UpdateSellerProfileCommand, SellerProfileDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public UpdateSellerProfileCommandHandler(
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

    public async Task<Result<SellerProfileDto, Error>> Handle(
        UpdateSellerProfileCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var profile = await _dbContext.Set<SellerProfile>()
            .FirstOrDefaultAsync(p => p.Id == userId, cancellationToken);

        if (profile is null)
            return UserErrors.SellerProfile.NotFound;

        var result = profile.Update(request.StoreName, request.StoreDescription, _clock.UtcNow);

        if (result.IsFailure)
            return result.Error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return profile.ToDto();
    }
}
