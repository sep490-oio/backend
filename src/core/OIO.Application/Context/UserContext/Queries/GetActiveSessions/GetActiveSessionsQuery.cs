using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;

namespace OIO.Application.Context.UserContext.Queries.GetActiveSessions;

public sealed record GetActiveSessionsQuery(
    Guid? CurrentDeviceId,
    PagedParameters PagedParameters)
    : IQuery<PagedList<UserSessionDto>>;