using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.ArchiveTermsDocument;

public sealed record ArchiveTermsDocumentCommand(
    Guid Id,
    string? Reason) : ICommand<TermsDocumentDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return ArchiveTermsDocumentCommand.Check()
            .WithOwnerName("ArchiveTermsDocument")
            .Field(Id)
            .NotEmptyGuid();
    }
}
