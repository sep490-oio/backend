using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.DTOs;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.SeedWork.Checks.Extensions;

namespace OIO.Application.UserContext.Queries.GetRoles;

public sealed record GetRolesQuery : IQuery<IReadOnlyList<RoleDto>>;