using MediatR;
using OIO.Application.Abstractions.Search;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;

namespace OIO.Infrastructure.Elasticsearch.EventHandlers;

public class UserSyncHandler : 
    INotificationHandler<UserCreatedEvent>,
    INotificationHandler<UserStatusChangedEvent>
{
    private readonly IElasticsearchSyncService _syncService;

    public UserSyncHandler(IElasticsearchSyncService syncService)
    {
        _syncService = syncService;
    }

    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        await _syncService.SyncUserAsync(Guid.Parse(notification.UserId), cancellationToken);
    }

    public async Task Handle(UserStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        await _syncService.SyncUserAsync(Guid.Parse(notification.UserId), cancellationToken);
    }
}
