using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetMyVerificationById;

public sealed record GetMyVerificationByIdQuery(Guid VerificationId) : IQuery<VerificationDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetMyVerificationByIdQuery.Check()
            .WithOwnerName("GetMyVerificationById")
            .Field(VerificationId).NotEmptyGuid();
    }
}
