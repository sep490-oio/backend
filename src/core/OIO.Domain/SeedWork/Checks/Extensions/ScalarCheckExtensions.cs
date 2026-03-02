using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.SeedWork.Checks.Extensions;

public static class ScalarCheckExtensions
{
    private static string SetString<T>(IEnumerable<T> set, Func<T, string>? formatter = null)
        => string.Join(", ", set.Select(x => formatter?.Invoke(x) ?? x?.ToString() ?? string.Empty));

    // -----------------------------------------------------------------
    // InSet / NotInSet (scalar)
    // -----------------------------------------------------------------
    extension<TOwner, T>(CheckField<TOwner, T> check)
    {
        public CheckField<TOwner, T> InSet(IEnumerable<T> set,
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

            var err = error ?? Error.ScalarInSet(
                property: check.FieldName,
                field: check.FieldName,
                setString: setStr,
                codePrefix: check.CodePrefix,
                isInvariant: check.IsInvariant,
                message: message);

            return check.Fail(err);
        }

        public CheckField<TOwner, T> NotInSet(IEnumerable<T> set,
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

            var err = error ?? Error.ScalarNotInSet(
                property: check.Property,
                field: check.FieldName,
                setString: setStr,
                codePrefix: check.CodePrefix,
                isInvariant: check.IsInvariant,
                message: message);

            return check.Fail(err);
        }
    }
}