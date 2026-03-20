using CSharpFunctionalExtensions;
using OIO.Application.Context.AuctionContext.Hubs;
using OIO.Domain.SeedWork.Checks;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Api.Hubs;

public sealed record HubCommandResult<T>(
    bool Success,
    T? Data,
    ErrorNotification? Error)
{
    public static HubCommandResult<T> FromResult(Result<T, Error> result)
        => result.IsSuccess
            ? new HubCommandResult<T>(true, result.Value, null)
            : FromError(result.Error);

    public static HubCommandResult<T> FromError(Error error)
        => new(false, default, ToErrorNotification(error));

    private static ErrorNotification ToErrorNotification(Error error)
    {
        if (error is not ViolationsError violationsError)
            return new ErrorNotification(error.Code, error.Message, null);

        var errorsDict = violationsError.Violations
            .GroupBy(e => ((ICheckError)e).PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.Message).ToArray());

        return new ErrorNotification(violationsError.Code, violationsError.Message, errorsDict);
    }
}
