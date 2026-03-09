namespace OIO.Application.Context.UserContext.DTOs;

public sealed record RoleDto(
    string Name,
    IReadOnlyList<string> Permissions);