using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.DTOs;

namespace OIO.Application.UserContext.Queries.GetPermissions;

public sealed record GetPermissionsQuery(PagedParameters PagedParameters) : IQuery<PagedList<PermissionDto>>;
