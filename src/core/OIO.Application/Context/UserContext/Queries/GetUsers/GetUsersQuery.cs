using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetUsers;

public sealed record GetUsersQuery(
    GetUsersFilterParameters Parameters) : IQuery<PagedList<UserListItemDto>>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetUsersQuery.Check()
            .WithOwnerName("GetUsers")
            .Field(Parameters.Status)
            .WhenHasValue(x => x.InSet(UserStatus.All.Select(y => y.Id)))
            .Field(Parameters.Role)
            .WhenHasValue(x => x.InSet(App.Roles.Catalogs.All))
            .Field(Parameters.SortBy)
            .WhenHasValue(x => x.Must(UserMappings.UserListItemDtoSortMapping.ValidateMappings));
    }
}