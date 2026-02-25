using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.DTOs;

namespace OIO.Application.UserContext.Queries.GetLoginHistory;

public sealed record GetLoginHistoryQuery(PagedParameters PagedParameters) : IQuery<PagedList<LoginHistoryDto>>;