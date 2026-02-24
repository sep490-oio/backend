using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.DTOs;
using OIO.Application.UserContext.Mappings;
using OIO.Application.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Queries.GetUserProfile;

internal sealed class GetUserProfileQueryHandler
    : IQueryHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetUserProfileQueryHandler(
        IDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<UserProfileDto, Error>> Handle(
        GetUserProfileQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return UserErrors.Auth.UserNotLoggedIn;

        var userProfile = await _dbContext.GetByIdAsync<UserProfile, UserId>( _currentUser.UserId, cancellationToken: cancellationToken);
        
        if (userProfile is null)
            return UserErrors.User.ProfileNotFound;

        return userProfile.ToDto();
    }
}