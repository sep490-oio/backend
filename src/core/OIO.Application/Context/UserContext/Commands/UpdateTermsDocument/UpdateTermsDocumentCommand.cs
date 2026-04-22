using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.UpdateTermsDocument;

public sealed record UpdateTermsDocumentCommand(
    Guid Id,
    Guid MediaUploadId) : ICommand<TermsDocumentDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return UpdateTermsDocumentCommand.Check()
            .WithOwnerName("UpdateTermsDocument")
            .Field(Id)
            .NotEmptyGuid()
            .Field(MediaUploadId)
            .NotEmptyGuid();
    }
}
