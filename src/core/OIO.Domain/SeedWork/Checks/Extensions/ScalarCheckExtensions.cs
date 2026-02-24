using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.SeedWork.Checks.Extensions;

public static class ScalarCheckExtensions
{
    private static string SetString<T>(IEnumerable<T> set, Func<T, string>? formatter = null)
        => string.Join(", ", set.Select(x => formatter?.Invoke(x) ?? x?.ToString() ?? string.Empty));

    // -----------------------------------------------------------------
    // InSet / NotInSet (scalar)
    // -----------------------------------------------------------------
    public static CheckField<TOwner, T> InSet<TOwner, T>(
        this CheckField<TOwner, T> check,
        IEnumerable<T> set,
        string? message = null,
        Error? error = null,
        IEqualityComparer<T>? comparer = null,
        Func<T, string>? formatter = null)
    {
        if (!check.ShouldContinue)
            return check;

        comparer ??= EqualityComparer<T>.Default;
        var enumerable = set as T[] ?? set.ToArray();
        
        var ok = enumerable.Any(x => comparer.Equals(x, check.Property));

        if (ok)
            return check;

        var setStr = SetString(enumerable, formatter);

        var err = error ?? check.ScalarInSetError(
            field: check.FieldName,
            setString: setStr,
            codePrefix: check.CodePrefix,
            isInvariant: check.IsInvariant,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, T> NotInSet<TOwner, T>(
        this CheckField<TOwner, T> check,
        IEnumerable<T> set,
        string? message = null,
        Error? error = null,
        IEqualityComparer<T>? comparer = null,
        Func<T, string>? formatter = null)
    {
        if (!check.ShouldContinue)
            return check;

        comparer ??= EqualityComparer<T>.Default;
        var enumerable = set as T[] ?? set.ToArray();
        var bad = enumerable.Any(x => comparer.Equals(x, check.Property));

        if (!bad)
            return check;

        var setStr = SetString(enumerable, formatter);

        var err = error ?? check.ScalarNotInSetError(
            field: check.FieldName,
            setString: setStr,
            codePrefix: check.CodePrefix,
            isInvariant: check.IsInvariant,
            message: message);

        return check.Fail(err);
    }

    // -----------------------------------------------------------------
    // Nullable helpers
    // -----------------------------------------------------------------
    public static CheckField<TOwner, T?> InSetIfHasValue<TOwner, T>(
        this CheckField<TOwner, T?> check,
        IEnumerable<T> set,
        string? message = null,
        Error? error = null,
        IEqualityComparer<T>? comparer = null,
        Func<T, string>? formatter = null)
        where T : struct
    {
        if (!check.ShouldContinue || !check.Property.HasValue)
            return check;

        comparer ??= EqualityComparer<T>.Default;
        var value = check.Property.Value;

        var enumerable = set as T[] ?? set.ToArray();
        var ok = enumerable.Any(x => comparer.Equals(x, value));
        if (ok)
            return check;

        var setStr = string.Join(", ", enumerable.Select(x => formatter?.Invoke(x) ?? x.ToString()));
        var err = error ?? check.ScalarInSetError(
            field: check.FieldName,
            setString: setStr,
            codePrefix: check.CodePrefix,
            isInvariant: check.IsInvariant,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, T?> NotInSetIfHasValue<TOwner, T>(
        this CheckField<TOwner, T?> check,
        IEnumerable<T> set,
        string? message = null,
        Error? error = null,
        IEqualityComparer<T>? comparer = null,
        Func<T, string>? formatter = null)
        where T : struct
    {
        if (!check.ShouldContinue || !check.Property.HasValue)
            return check;

        comparer ??= EqualityComparer<T>.Default;
        var value = check.Property.Value;

        var enumerable = set as T[] ?? set.ToArray();
        var bad = enumerable.Any(x => comparer.Equals(x, value));
        if (!bad)
            return check;

        var setStr = string.Join(", ", enumerable.Select(x => formatter?.Invoke(x) ?? x.ToString()));
        var err = error ?? check.ScalarNotInSetError(
            field: check.FieldName,
            setString: setStr,
            codePrefix: check.CodePrefix,
            isInvariant: check.IsInvariant,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, T?> InSetIfNotNull<TOwner, T>(
        this CheckField<TOwner, T?> check,
        IEnumerable<T> set,
        string? message = null,
        Error? error = null,
        IEqualityComparer<T>? comparer = null,
        Func<T, string>? formatter = null)
        where T : class
    {
        if (check.Property is null)
            return check;

        return check.NotNull().InSet(set, message, error, comparer, formatter)!;
    }

    public static CheckField<TOwner, T?> NotInSetIfNotNull<TOwner, T>(
        this CheckField<TOwner, T?> check,
        IEnumerable<T> set,
        string? message = null,
        Error? error = null,
        IEqualityComparer<T>? comparer = null,
        Func<T, string>? formatter = null)
        where T : class
    {
        if (check.Property is null)
            return check;

        return check.NotNull().NotInSet(set, message, error, comparer, formatter)!;
    }
}