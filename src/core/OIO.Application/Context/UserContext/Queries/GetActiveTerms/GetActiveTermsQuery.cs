using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;

namespace OIO.Application.Context.UserContext.Queries.GetActiveTerms;

public sealed record GetActiveTermsQuery : IQuery<IReadOnlyList<TermsDocumentDto>>;
