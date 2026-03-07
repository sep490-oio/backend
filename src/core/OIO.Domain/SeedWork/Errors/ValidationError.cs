using OIO.Domain.SeedWork.Checks;
using OIO.Domain.SeedWork.Errors.ErrorCatalogs;

namespace OIO.Domain.SeedWork.Errors;

[GenerateSerializer]
[Alias("OIO.Domain.SeedWork.Errors.ValidationError")]
public sealed record ValidationError(string PropertyName ,string Code , string Message) : Error(Code, Message, ErrorCatalog.Kind.Validation), ICheckError;