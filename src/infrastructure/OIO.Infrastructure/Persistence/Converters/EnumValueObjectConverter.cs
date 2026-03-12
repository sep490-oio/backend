using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OIO.Infrastructure.Persistence.Converters;

public static class EnumValueObjectConventionExtensions
{
    public static PropertyBuilder<TEnum> HasEnumConversion<TEnum>(
        this PropertyBuilder<TEnum> builder, int maxLength = 30)
        where TEnum : EnumValueObject<TEnum>
    {
        return builder
            .HasConversion(e => e.Id, id => EnumValueObject<TEnum>.FromId(id).Value)
            .HasMaxLength(maxLength);
    }
}
