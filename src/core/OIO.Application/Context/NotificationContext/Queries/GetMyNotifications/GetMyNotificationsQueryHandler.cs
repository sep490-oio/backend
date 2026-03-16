using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Extensions;
using OIO.Domain.Context.NotificationContext.Aggregates;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.NotificationContext.Queries.GetMyNotifications;

internal sealed class GetMyNotificationsQueryHandler 
    : IQueryHandler<GetMyNotificationsQuery, PagedList<NotificationDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMyNotificationsQueryHandler(
        IDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedList<NotificationDto>, Error>> Handle(
        GetMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {

        var query = _dbContext.Set<Notification>()
            .AsNoTracking()
            .Where(n => n.UserId == _currentUser.UserId)
            .OrderByDescending(n => n.CreatedAt);

        var count = await query.CountAsync(cancellationToken);

        var notifications = await query
            .Select(n => new NotificationDto(
                n.Id.Value,
                n.NotificationType,
                n.EventType,
                n.Title,
                n.Message,
                n.Priority.ToString(),
                n.Status.ToString(),
                n.EntityType,
                n.EntityId,
                n.Metadata,
                n.RelatedEntities,
                n.Actions,
                n.CreatedAt,
                n.ReadAt,
                n.ExpiresAt
            ))
            .ToPagedListAsync(count, request.Parameters, cancellationToken);

        return notifications;
    }
}
