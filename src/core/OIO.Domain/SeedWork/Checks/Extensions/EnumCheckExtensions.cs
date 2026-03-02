using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.SeedWork.Checks.Extensions;

public static class EnumCheckExtensions
{
    // ---------------------------------------------------------------
    // InEnum for enum-typed fields
    // ---------------------------------------------------------------
    public static CheckField<TOwner, TEnum> InEnum<TOwner, TEnum>(
        this CheckField<TOwner, TEnum> check,
        string? message = null,
        Error? error = null)
        where TEnum : struct, Enum
    {
        if (!check.ShouldContinue)
            return check;

        var ok = Enum.IsDefined(check.Property);

        if (ok)
            return check;

        var enumName = typeof(TEnum).Name;
        var valueStr = check.Property.ToString();

        var err = error ?? Error.InEnum(
            property: check.Property,
            field: check.FieldName,
            enumName: enumName,
            value: valueStr,
            codePrefix: check.CodePrefix,
            isInvariant: check.IsInvariant,
            message: message);

        return check.Fail(err);
    }

    // ---------------------------------------------------------------
    // InEnum for underlying numeric (int / long)
    // ---------------------------------------------------------------
    public static CheckField<TOwner, int> InEnum<TOwner, TEnum>(
        this CheckField<TOwner, int> check,
        string? message = null,
        Error? error = null)
        where TEnum : struct, Enum
    {
        if (!check.ShouldContinue)
            return check;

        var ok = Enum.IsDefined(typeof(TEnum), check.Property);
        if (ok)
            return check;

        var enumName = typeof(TEnum).Name;
        var valueStr = check.Property.ToString();

        var err = error ?? Error.InEnum(
            property: check.Property,
            field: check.FieldName,
            enumName: enumName,
            value: valueStr,
            codePrefix: check.CodePrefix,
            isInvariant: check.IsInvariant,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, long> InEnum<TOwner, TEnum>(
        this CheckField<TOwner, long> check,
        string? message = null,
        Error? error = null)
        where TEnum : struct, Enum
    {
        if (!check.ShouldContinue)
            return check;

        var ok = Enum.IsDefined(typeof(TEnum), check.Property);
        if (ok)
            return check;

        var enumName = typeof(TEnum).Name;
        var valueStr = check.Property.ToString();

        var err = error ?? Error.InEnum(
            property: check.Property,
            field: check.FieldName,
            enumName: enumName,
            value: valueStr,
            codePrefix: check.CodePrefix,
            isInvariant: check.IsInvariant,
            message: message);

        return check.Fail(err);
    }

    // ---------------------------------------------------------------
    // InEnum for string names (case-insensitive by default)
    // ---------------------------------------------------------------
    public static CheckField<TOwner, string> InEnum<TOwner, TEnum>(
        this CheckField<TOwner, string> check,
        bool ignoreCase = true,
        string? message = null,
        Error? error = null)
        where TEnum : struct, Enum
    {
        if (!check.ShouldContinue)
            return check;

        var ok = Enum.TryParse<TEnum>(check.Property, ignoreCase, out var parsed)
                 && Enum.IsDefined(parsed);

        if (ok)
            return check;

        var enumName = typeof(TEnum).Name;
        var valueStr = check.Property;

        var err = error ?? Error.InEnum(
            property: check.Property,
            field: check.FieldName,
            enumName: enumName,
            value: valueStr,
            codePrefix: check.CodePrefix,
            isInvariant: check.IsInvariant,
            message: message);

        return check.Fail(err);
    }
}