using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Extensions;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetLoginHistory;

internal sealed class GetLoginHistoryQueryHandler
    : IQueryHandler<GetLoginHistoryQuery, PagedList<LoginHistoryDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetLoginHistoryQueryHandler(
        IDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedList<LoginHistoryDto>, Error>> Handle(
        GetLoginHistoryQuery request,
        CancellationToken cancellationToken)
    {
        

        var items = await _dbContext.Set<UserLoginHistory>()
            .Where(u => u.UserId == _currentUser.UserId)
            .OrderByDescending(h => h.LoginAt)
            .Page(request.PagedParameters)
            .Select(h => new LoginHistoryDto(
                h.Id.Value,
                h.IpAddress.ToString(),
                h.UserAgent,
                h.LoginAt,
                h.Status.Id))
            .ToListAsync(cancellationToken);

        var totalCount = await _dbContext.Set<UserLoginHistory>()
            .Where(u => u.UserId == _currentUser.UserId)
            .CountAsync(cancellationToken);

        return  PagedList<LoginHistoryDto>.ToPagedList(
            items,
            totalCount, 
            request.PagedParameters);
    }
}