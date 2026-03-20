using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;

namespace OIO.Application.Context.NotificationContext.Queries.GetMyNotifications;

public sealed record GetMyNotificationsQuery(
    PagedParameters Parameters) : IQuery<PagedList<NotificationDto>>;
