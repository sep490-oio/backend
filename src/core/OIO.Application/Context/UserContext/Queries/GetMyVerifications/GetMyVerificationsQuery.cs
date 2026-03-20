using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;

namespace OIO.Application.Context.UserContext.Queries.GetMyVerifications;

public sealed record GetMyVerificationsQuery : IQuery<IReadOnlyCollection<VerificationSummaryDto>>;
