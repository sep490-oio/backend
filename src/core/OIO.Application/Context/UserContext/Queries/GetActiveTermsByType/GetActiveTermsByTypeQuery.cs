using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;

namespace OIO.Application.Context.UserContext.Queries.GetActiveTermsByType;

public sealed record GetActiveTermsByTypeQuery(string Type) : IQuery<TermsDocumentDto>;
