using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetUserByIdQuery.Check()
            .WithOwnerName("GetUserById")
            .Field(UserId)
            .NotEmptyGuid();
    }
}