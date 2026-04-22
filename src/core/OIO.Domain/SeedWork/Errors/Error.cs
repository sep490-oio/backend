using OIO.Domain.SeedWork.Errors.ErrorCatalogs;
using OIO.Domain.SeedWork.Utils;

namespace OIO.Domain.SeedWork.Errors;

[GenerateSerializer]
public record Error
{
    [Id(0)]
    public string Code { get; protected init; } = string.Empty;
    [Id(1)]
    public string Message { get; protected init; } = string.Empty;
    [Id(2)]
    public string Kind { get; protected init; } = string.Empty;

    // Optional structured payload surfaced to the HTTP response under the JSON "metadata"
    // property. Wire format:
    //   { "code": "PaymentMethod.Duplicate", "description": "...", "metadata": { "conflictingMethodId": "<guid>", "existingIsActive": true } }
    // Default null for existing constructors so all existing call sites are unaffected.
    [Id(3)]
    public IReadOnlyDictionary<string, object>? Metadata { get; protected init; }

    protected Error(string code, string message, string kind)
    {
        Code = code;
        Message = message;
        Kind = kind;
    }

    protected Error(string code, string message, string kind, IReadOnlyDictionary<string, object>? metadata)
    {
        Code = code;
        Message = message;
        Kind = kind;
        Metadata = metadata;
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

    /// <summary>
    /// Creates a Conflict error with a structured metadata payload. The dictionary is surfaced
    /// to the HTTP response body under the JSON "metadata" property, e.g.:
    /// { "code": "PaymentMethod.Duplicate", "description": "...", "metadata": { "conflictingMethodId": "&lt;guid&gt;", "existingIsActive": true } }
    /// </summary>
    public static Error Conflict(string code, string description, IReadOnlyDictionary<string, object> metadata) =>
        new(code, description, ErrorCatalog.Kind.Conflict, metadata);

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