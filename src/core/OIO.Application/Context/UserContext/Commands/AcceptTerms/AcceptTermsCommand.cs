using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.AcceptTerms;

public sealed record AcceptTermsCommand(
    Guid TermDocumentId,
    string? IpAddress,
    string? UserAgent) : ICommand<TermsAcceptanceDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return AcceptTermsCommand.Check()
            .WithOwnerName("AcceptTerms")
            .Field(TermDocumentId)
            .NotEmptyGuid();
    }
}
