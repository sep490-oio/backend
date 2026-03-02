using System.Collections;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.SeedWork.Checks.Extensions;

public static class CollectionCheckExtensions
{
    // ---------- helpers ----------
    private static int CountOf(IEnumerable? seq)
    {
        switch (seq)
        {
            case null:
                return 0;
            case ICollection c:
                return c.Count;
        }

        // Try generic IReadOnlyCollection<T>
        var t = seq.GetType();
        foreach (var it in t.GetInterfaces())
        {
            if (!it.IsGenericType || it.GetGenericTypeDefinition() != typeof(IReadOnlyCollection<>)) 
                
                continue;
            var countProp = it.GetProperty("Count");
            
            if (countProp?.GetValue(seq) is int n) return n;
        }

        return seq.Cast<object?>().Count();
    }

    private static string SetString<T>(IEnumerable<T> set, Func<T, string>? formatter = null)
        => string.Join(", ", set.Select(x => formatter?.Invoke(x) ?? x?.ToString() ?? string.Empty));

    private static bool ContainsItem<T>(IEnumerable<T> seq, T item, IEqualityComparer<T>? comparer)
    {
        comparer ??= EqualityComparer<T>.Default;
        
        return seq.Any(x => comparer.Equals(x, item));
    }

    private static bool TryGetFirstDuplicate<T>(IEnumerable<T> seq, out T duplicate, IEqualityComparer<T>? comparer)
    {
        comparer ??= EqualityComparer<T>.Default;
        var seen = new HashSet<T>(comparer);
        foreach (var x in seq)
        {
            if (seen.Add(x))
                continue;
            
            duplicate = x;
            return true;
        }

        duplicate = default!;
        return false;
    }

    // -----------------------------------------------------------------
    // NotContains
    // -----------------------------------------------------------------
    public static CheckField<TOwner, IEnumerable<T>> NotContains<TOwner, T>(
        this CheckField<TOwner, IEnumerable<T>> check,
        T item,
        string? message = null,
        Error? error = null,
        IEqualityComparer<T>? comparer = null,
        Func<T, string>? itemToString = null)
    {
        if (!check.ShouldContinue || !ContainsItem(check.Property ?? [], item, comparer)) 
            return check;

        var itemStr = itemToString?.Invoke(item) ?? item?.ToString() ?? string.Empty;

        var err = error ?? Error.NotContains(
            property: check.Property,
            item: itemStr,
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    // -----------------------------------------------------------------
    // InSet / NotInSet
    // (Semantics: all items must be in set OR none of items in set)
    // -----------------------------------------------------------------
    public static CheckField<TOwner, IEnumerable<T>> InSet<TOwner, T>(
        this CheckField<TOwner, IEnumerable<T>> check,
        IEnumerable<T> allowedSet,
        string? message = null,
        Error? error = null,
        IEqualityComparer<T>? comparer = null,
        Func<T, string>? formatter = null)
    {
        if (!check.ShouldContinue) return check;

        comparer ??= EqualityComparer<T>.Default;
        
        var allowed = new HashSet<T>(allowedSet ?? [], comparer);

        // all items must be in allowed
        if ((check.Property ?? []).All(x => allowed.Contains(x))) 
            return check;
        
        var setStr = SetString(allowed, formatter);
        var err = error ?? Error.InSet(
            property: check.Property,
            setString: setStr,
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);

    }

    public static CheckField<TOwner, IEnumerable<T>> NotInSet<TOwner, T>(
        this CheckField<TOwner, IEnumerable<T>> check,
        IEnumerable<T> blockedSet,
        string? message = null,
        Error? error = null,
        IEqualityComparer<T>? comparer = null,
        Func<T, string>? formatter = null)
    {
        if (!check.ShouldContinue) return check;

        comparer ??= EqualityComparer<T>.Default;
        var blocked = new HashSet<T>(blockedSet ?? [], comparer);

        if (!(check.Property ?? []).Any(x => blocked.Contains(x))) 
            return check;
        
        var setStr = SetString(blocked, formatter);
        var err = error ?? Error.NotInSet(
            property: check.Property,
            setString: setStr,
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);

    }

    // -----------------------------------------------------------------
    // NoDuplicates
    // -----------------------------------------------------------------
    public static CheckField<TOwner, IEnumerable<T>> NoDuplicates<TOwner, T>(
        this CheckField<TOwner, IEnumerable<T>> check,
        string? message = null,
        Error? error = null,
        IEqualityComparer<T>? comparer = null,
        Func<T, string>? itemToString = null)
    {
        if (!check.ShouldContinue) return check;

        var seq = check.Property ?? [];
        if (!TryGetFirstDuplicate(seq, out var dup, comparer))
            return check;

        var dupStr = itemToString?.Invoke(dup) ?? dup?.ToString() ?? string.Empty;

        var err = error ?? Error.NoDuplicates(
            property: check.Property,
            duplicate: dupStr,
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    // -----------------------------------------------------------------
    // CountMin / CountMax / CountBetween
    // -----------------------------------------------------------------
    public static CheckField<TOwner, IEnumerable> CountMin<TOwner>(
        this CheckField<TOwner, IEnumerable> check,
        int min,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue) return check;

        var count = CountOf(check.Property);
        if (count >= min) return check;

        var err = error ?? Error.CountMin(
            property: check.Property,
            min: min,
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, IEnumerable> CountMax<TOwner>(
        this CheckField<TOwner, IEnumerable> check,
        int max,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue) return check;

        var count = CountOf(check.Property);
        if (count <= max) return check;

        var err = error ?? Error.CountMax(
            property: check.Property,
            max: max,
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }

    public static CheckField<TOwner, IEnumerable> CountBetween<TOwner>(
        this CheckField<TOwner, IEnumerable> check,
        int min,
        int max,
        string? message = null,
        Error? error = null)
    {
        if (!check.ShouldContinue) return check;

        var count = CountOf(check.Property);
        if (count >= min && count <= max) return check;

        var err = error ?? Error.CountBetween(
            property: check.Property,
            min: min,
            max: max,
            isInvariant: check.IsInvariant,
            codePrefix: check.OwnerName,
            propertyName: check.PropertyName,
            field: check.FieldName,
            message: message);

        return check.Fail(err);
    }
}