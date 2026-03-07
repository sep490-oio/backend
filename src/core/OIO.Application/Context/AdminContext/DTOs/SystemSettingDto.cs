namespace OIO.Application.Context.AdminContext.DTOs;

public sealed record SystemSettingDto(
    string Key,
    object? Value,
    string ValueType,
    string? Description,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    string? ModifiedBy);