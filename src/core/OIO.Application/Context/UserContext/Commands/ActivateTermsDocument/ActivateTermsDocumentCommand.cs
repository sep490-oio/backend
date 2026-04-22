using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.ActivateTermsDocument;

public sealed record ActivateTermsDocumentCommand(Guid Id) : ICommand<TermsDocumentDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return ActivateTermsDocumentCommand.Check()
            .WithOwnerName("ActivateTermsDocument")
            .Field(Id)
            .NotEmptyGuid();
    }
}
