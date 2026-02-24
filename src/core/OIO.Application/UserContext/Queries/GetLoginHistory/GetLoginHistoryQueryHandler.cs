using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Extensions;
using OIO.Application.UserContext.DTOs;
using OIO.Application.UserContext.Mappings;
using OIO.Application.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.Repositories;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Queries.GetLoginHistory;

internal sealed class GetLoginHistoryQueryHandler
    : IQueryHandler<GetLoginHistoryQuery, PagedResult<LoginHistoryDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetLoginHistoryQueryHandler(
        IUserRepository userRepository,
        IDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<LoginHistoryDto>, Error>> Handle(
        GetLoginHistoryQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return UserErrors.Auth.UserNotLoggedIn;

        var items = await _dbContext.Set<UserLoginHistory>()
            .Where(u => u.UserId == _currentUser.UserId)
            .OrderByDescending(h => h.LoginAt)
            .Paged(request.PagedParameters)
            .Select(UserLoginHistory.UserLoginHistoryProjectToDto())
            .ToListAsync(cancellationToken);

        var totalCount = await _dbContext.Set<UserLoginHistory>()
            .Where(u => u.UserId == _currentUser.UserId)
            .CountAsync(cancellationToken);

        return  PagedResult<LoginHistoryDto>.ToPagedList(
            items,
            totalCount, 
            request.PagedParameters);
    }
}