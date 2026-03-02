using OIO.Domain.SeedWork.Errors.ErrorCatalogs;
using OIO.Domain.SeedWork.Utils;

namespace OIO.Domain.SeedWork.Errors;

public record Error
{
    public string Code { get; protected init; } = string.Empty;
    public string Message { get; protected init; } = string.Empty;
    public string Kind { get; protected init; } = string.Empty;
    
    protected Error(string code, string message, string kind)
    {
        Code = code;
        Message = message;
        Kind = kind;
    }
    
    protected Error()
    {}
    
    public string Serialize(string separator = Constant.DefaultErrorSerializeSeparator)
        => $"{Code}{separator}{Kind}{separator}{Message}";
    
    public static Error Deserialize(string serialized, string separator = Constant.DefaultErrorSerializeSeparator)
    {
        var data = serialized.Split([separator], StringSplitOptions.RemoveEmptyEntries);
        return data.Length < 3
            ? throw new InvalidErrorException($"Invalid error serialization: '{serialized}'")
            : new Error(code: data[0], message: data[2], kind: data[1]);
    }
    
    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorCatalog.Kind.Conflict);

    public static ValidationError Validation(string propertyName, string code, string description) =>
        new ValidationError(propertyName, code, description);

    public static Error Unauthorized(string code, string description) =>
        new(code, description, ErrorCatalog.Kind.Unauthorized);

    public static Error Forbidden(string code, string description) =>
        new(code, description, ErrorCatalog.Kind.Forbidden);

    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorCatalog.Kind.NotFound);

    public static InvariantError Invariant(string propertyName, string code, string description) =>
        new InvariantError(propertyName, code, description);

    public static Error Unexpected(string code, string description) =>
        new(code, description, ErrorCatalog.Kind.Unexpected);

    public static Error Unavailable(string code, string description) =>
        new(code, description, ErrorCatalog.Kind.Unavailable);

    public static Error Timeout(string code, string description) =>
        new(code, description, ErrorCatalog.Kind.Timeout);
}