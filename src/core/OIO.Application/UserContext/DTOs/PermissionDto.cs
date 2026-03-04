using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.UserContext.DTOs;

public sealed record PermissionDto( 
    PermissionId Id,
    string PermissionCode);