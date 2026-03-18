using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.UserContext.Errors;

public static class TermsErrors
{
    public static readonly Func<TermsDocumentId, Error> TermsDocumentNotFound = id => Error.NotFound(
        code: "Terms.Document.NotFound",
        description: $"Terms document '{id}' was not found.");

    public static readonly Func<string, Error> ActiveTermsByTypeNotFound = type => Error.NotFound(
        code: "Terms.Document.Active.NotFound",
        description: $"No active terms document found for type '{type}'.");

    public static readonly Error CannotAcceptInactiveTerms = Error.Forbidden(
        code: "Terms.Accept.Inactive",
        description: "Only active terms documents can be accepted.");

    public static readonly Func<TermsDocumentId, Error> AlreadyAccepted = termDocumentId => Error.Conflict(
        code: "Terms.Accept.AlreadyAccepted",
        description: $"You have already accepted terms document '{termDocumentId}'.");
}