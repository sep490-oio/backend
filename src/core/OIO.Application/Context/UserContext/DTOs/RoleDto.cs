namespace OIO.Application.Context.UserContext.DTOs;

public sealed record RoleDto(
    int Id,
    string Name,
    IReadOnlyList<string> Permissions);