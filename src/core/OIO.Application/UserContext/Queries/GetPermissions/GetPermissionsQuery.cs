using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.DTOs;

namespace OIO.Application.UserContext.Queries.GetPermissions;

public sealed record GetPermissionsQuery : IQuery<IReadOnlyList<PermissionDto>>;
