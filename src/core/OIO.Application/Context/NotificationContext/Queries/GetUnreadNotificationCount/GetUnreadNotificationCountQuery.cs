using OIO.Application.Abstractions.Messaging;

namespace OIO.Application.Context.NotificationContext.Queries.GetUnreadNotificationCount;

public sealed record GetUnreadNotificationCountQuery() : IQuery<GetUnreadNotificationCountResponse>;

public sealed record GetUnreadNotificationCountResponse(int Count);
