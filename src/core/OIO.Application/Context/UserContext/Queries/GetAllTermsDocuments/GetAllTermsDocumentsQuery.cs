using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;

namespace OIO.Application.Context.UserContext.Queries.GetAllTermsDocuments;

public sealed record GetAllTermsDocumentsQuery(string? Type = null, bool? IsActive = null) : IQuery<IReadOnlyList<TermsDocumentDto>>;
