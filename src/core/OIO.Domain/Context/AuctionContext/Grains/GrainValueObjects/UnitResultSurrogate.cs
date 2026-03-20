using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.AuctionContext.Grains.GrainValueObjects;

[GenerateSerializer]
public struct UnitResultSurrogate<TE>
{
    [Id(0)] public bool IsSuccess { get; set; }
    [Id(1)] public TE Error { get; set; }
}

[RegisterConverter]
public sealed class UnitResultConverter<TE> : IConverter<UnitResult<TE>, UnitResultSurrogate<TE>>
{
    public UnitResult<TE> ConvertFromSurrogate(in UnitResultSurrogate<TE> surrogate)
    {
        return surrogate.IsSuccess
            ? UnitResult.Success<TE>()
            : UnitResult.Failure(surrogate.Error);
    }

    public UnitResultSurrogate<TE> ConvertToSurrogate(in UnitResult<TE> value)
    {
        return new UnitResultSurrogate<TE>
        {
            IsSuccess = value.IsSuccess,
            Error = value.IsFailure ? value.Error : default!
        };
    }
}
