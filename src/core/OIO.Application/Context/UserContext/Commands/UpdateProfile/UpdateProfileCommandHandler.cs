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
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.UpdateProfile;

internal sealed class UpdateProfileCommandHandler
    : ICommandHandler<UpdateProfileCommand, UserProfileDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public UpdateProfileCommandHandler(
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

    public async Task<Result<UserProfileDto, Error>> Handle(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;

        var (_, isFailure, avatarUrl, error) = AvatarUrl.Create(request.AvatarUrl);
        
        if (request.AvatarUrl is not null && isFailure)
            return error;

        var user = await _dbContext.GetByIdAsync<User, UserId>(
            _currentUser.UserId,
            query => query.Include(x => x.Profile),
            cancellationToken);
        
        if (user is null)
            return UserErrors.User.NotFound(_currentUser.UserId);

        //if 'request.Gender' is null then it value when in string interpolation will be "",
        //then FromId will return null, and GetValueOrDefault will return
        var gender = Gender.FromId($"{request.Gender}").GetValueOrDefault();
        
        var personName = PersonName.Create(request.FirstName ?? user.Profile.Name?.FirstName, request.LastName ?? user.Profile.Name?.LastName, request.DisplayName ?? user.Profile.Name?.DisplayName);
        
        if (request.AvatarUrl is not null && isFailure)
            return error;
        
        var updateProfileR = user.UpdateProfile(
            name: personName,
            avatarUrl: avatarUrl,
            dateOfBirth: request.DateOfBirth,
            gender: gender,
            now: nowUtc);

        if (updateProfileR.IsFailure)
        {
            return updateProfileR.Error;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.Profile!.ToDto();
    }
}