using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using OIO.Domain.SeedWork.Errors;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace OIO.Api.Filters.Http;

internal sealed record CachedHttpCommandResult<TPayload>(
    bool Success,
    TPayload? Payload,
    string? SerializedError)
{
    public static CachedHttpCommandResult<TPayload> From(Result<TPayload, Error> result) =>
        result.IsSuccess
            ? new CachedHttpCommandResult<TPayload>(true, result.Value, null)
            : new CachedHttpCommandResult<TPayload>(false, default, result.Error.Serialize());

    public static CachedHttpCommandResult<TPayload> FromError(Error error) =>
        new(false, default, error.Serialize());

    public IResult ToOkResult()
    {
        if (Success)
            return TypedResults.Ok(Payload);

        return ToProblemResult();
    }

    public IResult ToCreatedResult()
    {
        if (Success)
            return TypedResults.Created(string.Empty, Payload);

        return ToProblemResult();
    }

    internal ProblemHttpResult ToProblemResult()
    {
        var error = SerializedError is null
            ? Error.Unexpected("Idempotency.CacheCorrupted", "Cached idempotent response is invalid.")
            : Error.Deserialize(SerializedError);

        return error.ToProblemDetails();
    }
}
