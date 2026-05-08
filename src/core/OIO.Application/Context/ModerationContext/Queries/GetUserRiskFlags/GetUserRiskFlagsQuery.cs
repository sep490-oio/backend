using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;

namespace OIO.Application.Context.ModerationContext.Queries.GetUserRiskFlags;

public sealed record GetUserRiskFlagsQuery(Guid UserId) : IQuery<IReadOnlyList<UserRiskFlagDto>>;
