using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.DTOs;

namespace OIO.Application.UserContext.Queries.GetActiveSessions;

public sealed record GetActiveSessionsQuery(
    Guid? CurrentDeviceId,
    PagedParameters PagedParameters)
    : IQuery<PagedList<UserSessionDto>>;