using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetVerificationById;

public sealed record GetVerificationByIdQuery(Guid VerificationId) : IQuery<VerificationDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetVerificationByIdQuery.Check()
            .WithOwnerName("GetVerificationById")
            .Field(VerificationId).NotEmptyGuid();
    }
}
