namespace OIO.Application.Context.UserContext.DTOs;

public sealed record UserProfileDto(
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? FullName,
    string? AvatarUrl,
    DateOnly? DateOfBirth,
    string? Gender);