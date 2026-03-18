using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.NotificationContext.Aggregates;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.NotificationContext.Queries.GetUnreadNotificationCount;

internal sealed class GetUnreadNotificationCountQueryHandler 
    : IQueryHandler<GetUnreadNotificationCountQuery, GetUnreadNotificationCountResponse>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetUnreadNotificationCountQueryHandler(
        IDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<GetUnreadNotificationCountResponse, Error>> Handle(
        GetUnreadNotificationCountQuery request,
        CancellationToken cancellationToken)
    {
        var count = await _dbContext.Set<Notification>()
            .Where(n => n.UserId == _currentUser.UserId && n.Status == NotificationStatus.Unread)
            .CountAsync(cancellationToken);

        return new GetUnreadNotificationCountResponse(count);
    }
}
