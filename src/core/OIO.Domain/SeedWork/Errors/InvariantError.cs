using OIO.Domain.SeedWork.Checks;
using OIO.Domain.SeedWork.Errors.ErrorCatalogs;

namespace OIO.Domain.SeedWork.Errors;

public sealed record InvariantError(string PropertyName ,string Code , string Message) : Error(Code, Message, ErrorCatalog.Kind.Invariant), ICheckError;