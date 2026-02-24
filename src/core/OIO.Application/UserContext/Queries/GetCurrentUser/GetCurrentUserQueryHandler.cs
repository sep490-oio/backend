using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.DTOs;
using OIO.Application.UserContext.Mappings;
using OIO.Application.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Queries.GetCurrentUser;

internal sealed class GetCurrentUserQueryHandler
    : IQueryHandler<GetCurrentUserQuery, UserDto>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetCurrentUserQueryHandler(
        IDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<UserDto, Error>> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return UserErrors.Auth.UserNotLoggedIn;

        var user = await _dbContext.GetByIdAsync<User, UserId>(
            _currentUser.UserId,
            queryBuilder: query => query.Include(x => x.Profile),
            cancellationToken: cancellationToken);
        
        if (user is null)
            return UserErrors.User.NotFound(_currentUser.UserId);

        return user.ToDto();
    }
}