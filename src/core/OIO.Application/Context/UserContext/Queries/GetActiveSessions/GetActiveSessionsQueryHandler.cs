using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetActiveSessions;

internal sealed class GetActiveSessionsQueryHandler
    : IQueryHandler<GetActiveSessionsQuery, PagedList<UserSessionDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public GetActiveSessionsQueryHandler(
        IDbContext dbContext,
        ICurrentUser currentUser,
        IClock clock)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<PagedList<UserSessionDto>, Error>> Handle(
        GetActiveSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var nowUtc =  _clock.UtcNow;
        

        var sessions = await _dbContext.Set<UserSession>()
            .Where(t => t.UserId == _currentUser.UserId && t.IsActive && nowUtc < t.ExpiresAt)
            .OrderByDescending(f => f.LastRotatedAt)
            .Select(f => new UserSessionDto(
                SessionId: f.Id.Value,
                DeviceId: f.DeviceId,
                UserAgent: f.UserAgent,
                IpAddress: f.IpAddress.ToString(),
                IsActive: f.IsActive,
                IsCurrentDevice: f.DeviceId == request.CurrentDeviceId,
                CreatedAt: f.CreatedAt,
                LastRotatedAt: f.LastRotatedAt,
                SlidingExpiresAt: f.ExpiresAt,
                AbsoluteExpiresAt: f.AbsoluteExpiresAt,
                IsNearingAbsoluteExpiration: f.IsNearingAbsoluteExpiration(nowUtc),
                RemainingAbsoluteTime: f.RemainingAbsoluteTime(nowUtc)))
            .ToListAsync(cancellationToken);
        
        var totalCount = await _dbContext.Set<UserLoginHistory>()
            .Where(u => u.UserId == _currentUser.UserId).CountAsync(cancellationToken);

        return  PagedList<UserSessionDto>.ToPagedList(
            sessions,
            totalCount, 
            request.PagedParameters);
            
    }
}