using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;

namespace OIO.Application.Context.UserContext.Queries.GetLoginHistory;

public sealed record GetLoginHistoryQuery(PagedParameters PagedParameters) : IQuery<PagedList<LoginHistoryDto>>;