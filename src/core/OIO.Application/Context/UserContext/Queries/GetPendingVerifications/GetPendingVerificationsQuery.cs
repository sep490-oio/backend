using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;

namespace OIO.Application.Context.UserContext.Queries.GetPendingVerifications;

public record GetPendingVerificationsQueryFilter : PagedParameters;

public sealed record GetPendingVerificationsQuery(GetPendingVerificationsQueryFilter Parameters) : IQuery<PagedList<VerificationSummaryDto>>;
