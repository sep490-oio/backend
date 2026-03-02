using System.Globalization;
using System.Numerics;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.SeedWork.Checks.Extensions;

public static class NumericExtensions
{
    #region Positive / Negative / NonPositive / NonNegative / Zero / NonZero

    public static CheckField<TOwner, T> Positive<TOwner, T>(
        this CheckField<TOwner, T> check,
        string? message = null,
        Error? error = null)
        where T : INumber<T>
    {
        if (!check.ShouldContinue || check.Property > T.Zero) 
            return check;

        var err = error ?? Error.Positive(
            property: check.Property,
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, T> Negative<TOwner, T>(
        this CheckField<TOwner, T> check,
        string? message = null,
        Error? error = null)
        where T : INumber<T>
    {
        if (!check.ShouldContinue || check.Property < T.Zero)
            return check;

        var err = error ?? Error.Negative(
            property: check.Property,
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, T> NonPositive<TOwner, T>(
        this CheckField<TOwner, T> check,
        string? message = null,
        Error? error = null)
        where T : INumber<T>
    {
        if (!check.ShouldContinue || check.Property <= T.Zero)
            return check;

        var err = error ?? Error.NonPositive(
            property: check.Property,
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, T> NonNegative<TOwner, T>(
        this CheckField<TOwner, T> check,
        string? message = null,
        Error? error = null)
        where T : INumber<T>
    {
        if (!check.ShouldContinue || check.Property >= T.Zero)
            return check;

        var err = error ?? Error.NonNegative(
            property:check.Property,
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, T> Zero<TOwner, T>(
        this CheckField<TOwner, T> check,
        string? message = null,
        Error? error = null)
        where T : INumber<T>
    {
        if (!check.ShouldContinue || check.Property == T.Zero)
            return check;

        var err = error ?? Error.Zero(
            property: check.Property,
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, T> NonZero<TOwner, T>(
        this CheckField<TOwner, T> check,
        string? message = null,
        Error? error = null)
        where T : INumber<T>
    {
        if (!check.ShouldContinue || check.Property != T.Zero)
            return check;

        var err = error ?? Error.NonZero(
            property: check.Property,
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    #endregion
    
    #region MultipleOf

    public static CheckField<TOwner, T> MultipleOf<TOwner, T>(
        this CheckField<TOwner, T> check,
        T step,
        string? message = null,
        Error? error = null)
        where T : INumber<T>
    {
        if (!check.ShouldContinue) return check;

        // Step must be non-zero to make sense; if step == 0, treat as invalid rule input (throw)
        if (step == T.Zero)
            throw new ArgumentOutOfRangeException(nameof(step), "step must be non-zero.");

        // For INumber<T>, modulo (%) is available
        if (check.Property % step == T.Zero) 
            return check;

        var err = error ?? Error.MultipleOf(
            property: check.Property,
            step: step,
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    #endregion

    #region PrecisionScale

    public static CheckField<TOwner, T> PrecisionScale<TOwner, T>(
        this CheckField<TOwner, T> check,
        int precision,
        int scale,
        string? message = null,
        Error? error = null)
        where T : INumber<T>
    {
        if (!check.ShouldContinue) return check;

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(precision);
        ArgumentOutOfRangeException.ThrowIfNegative(scale);
        
        if (scale > precision) 
            throw new ArgumentOutOfRangeException(nameof(scale), "scale must be <= precision.");

        if (IsWithinPrecisionScale(check.Property, precision, scale))
            return check;

        var err = error ?? Error.PrecisionScale(
            property: check.Property,
            precision: precision,
            scale: scale,
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    private static bool IsWithinPrecisionScale<T>(T value, int precision, int scale)
        where T : INumber<T>
    {
        // Best effort:
        // - decimal: exact digits
        // - other: invariant string representation (maybe scientific notation -> handle roughly)
        if (value is decimal dec)
            return DecimalWithinPrecisionScale(dec, precision, scale);

        var s = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0";
        s = s.Trim();

        // handle scientific notation loosely: if "1E-5" -> treat as precision fail unless scale allows
        if (s.Contains('E') || s.Contains('e'))
        {
            // You can improve this by parsing exponent; for now, be conservative:
            // try convert to decimal if possible
            return decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) && DecimalWithinPrecisionScale(d, precision, scale);
        }

        // strip sign
        if (s.StartsWith("-", StringComparison.Ordinal)) s = s[1..];
        if (s.StartsWith("+", StringComparison.Ordinal)) s = s[1..];

        var parts = s.Split('.', 2);
        var intPart = parts[0].TrimStart('0');
        var fracPart = parts.Length == 2 ? parts[1].TrimEnd('0') : "";

        var intDigits = intPart.Length == 0 ? 1 : intPart.Length; // "0" counts as 1 digit
        var fracDigits = fracPart.Length;

        if (fracDigits > scale) return false;
        return intDigits + fracDigits <= precision;
    }

    private static bool DecimalWithinPrecisionScale(decimal value, int precision, int scale)
    {
        value = Math.Abs(value);

        // Normalize by removing trailing zeros to not punish 1.2300
        var s = value.ToString(CultureInfo.InvariantCulture);
        var parts = s.Split('.', 2);
        var intPart = parts[0].TrimStart('0');
        var fracPart = parts.Length == 2 ? parts[1].TrimEnd('0') : "";

        var intDigits = intPart.Length == 0 ? 1 : intPart.Length;
        var fracDigits = fracPart.Length;

        if (fracDigits > scale) return false;
        return intDigits + fracDigits <= precision;
    }

    #endregion
}