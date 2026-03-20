using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.CreateTermsDocument;

public sealed record CreateTermsDocumentCommand(
    string Type,
    Guid MediaUploadId) : ICommand<TermsDocumentDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return CreateTermsDocumentCommand.Check()
            .WithOwnerName("CreateTermsDocument")
            .Field(Type)
            .NotWhiteSpace()
            .MaxLength(50)
            .Field(MediaUploadId)
            .NotEmptyGuid()
            .ToViolationsError();
    }
}
