using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OIO.Domain.Context.UserContext.ValueObjects;

namespace OIO.Infrastructure.Persistence.Converters;

public class EnumValueObjectConverter<TEnum> : ValueConverter<TEnum, string>
    where TEnum : EnumValueObject<TEnum>
{
    public EnumValueObjectConverter()
        : base(
            enumeration => enumeration.Id,
            value => EnumValueObject<TEnum>.FromId(value).GetValueOrThrow())
    { }
}
