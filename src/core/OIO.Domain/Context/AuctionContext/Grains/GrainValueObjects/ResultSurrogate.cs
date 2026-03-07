using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.AuctionContext.Grains.GrainValueObjects;

[GenerateSerializer]
public struct ResultSurrogate<T, TE>
{
    [Id(0)] public bool IsSuccess { get; set; }
    [Id(1)] public T Value { get; set; }
    [Id(2)] public TE Error { get; set; }
}


[RegisterConverter]
public sealed class ResultConverter<T, TE> : IConverter<Result<T, TE>, ResultSurrogate<T, TE>>
{
    public Result<T, TE> ConvertFromSurrogate(in ResultSurrogate<T, TE> surrogate)
    {
        return surrogate.IsSuccess 
            ? Result.Success<T, TE>(surrogate.Value) 
            : Result.Failure<T, TE>(surrogate.Error);
    }

    public ResultSurrogate<T, TE> ConvertToSurrogate(in Result<T, TE> value)
    {
        return new ResultSurrogate<T, TE>
        {
            IsSuccess = value.IsSuccess,
            Value = value.IsSuccess ? value.Value : default!,
            Error = value.IsFailure ? value.Error : default!
        };
    }
}