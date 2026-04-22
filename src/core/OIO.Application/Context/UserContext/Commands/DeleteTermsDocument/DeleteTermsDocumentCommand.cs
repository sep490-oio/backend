using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.DeleteTermsDocument;

public sealed record DeleteTermsDocumentCommand(Guid Id) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return DeleteTermsDocumentCommand.Check()
            .WithOwnerName("DeleteTermsDocument")
            .Field(Id)
            .NotEmptyGuid();
    }
}
