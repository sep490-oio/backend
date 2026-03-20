using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;

namespace OIO.Application.Context.UserContext.Queries.GetMyAcceptedTerms;

public sealed record GetMyAcceptedTermsQuery : IQuery<IReadOnlyList<TermsAcceptanceDto>>;
