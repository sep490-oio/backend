using System.Reflection;

namespace OIO.Domain.SeedWork.Entities;

public abstract class Enumeration<TEnum> : IEquatable<Enumeration<TEnum>>
    where TEnum : Enumeration<TEnum>
{
    private static readonly Lazy<IReadOnlyDictionary<string, TEnum>> EnumsByValue = new(
        () => GetAll().ToDictionary(e => e.Value, StringComparer.OrdinalIgnoreCase));

    public string Value { get; }
    public string DisplayName { get; }

    protected Enumeration(string value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }

    public static IReadOnlyCollection<TEnum> GetAll()
    {
        return typeof(TEnum)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType == typeof(TEnum))
            .Select(f => (TEnum)f.GetValue(null)!)
            .ToList()
            .AsReadOnly();
    }

    public static TEnum FromValue(string value)
    {
        if (EnumsByValue.Value.TryGetValue(value, out var result))
            return result;

        throw new ArgumentException(
            $"'{value}' is not a valid value for {typeof(TEnum).Name}. " +
            $"Valid values: {string.Join(", ", EnumsByValue.Value.Keys)}");
    }

    public static bool TryFromValue(string value, out TEnum? result)
    {
        return EnumsByValue.Value.TryGetValue(value, out result);
    }

    public override string ToString() => Value;

    public bool Equals(Enumeration<TEnum>? other)
    {
        if (other is null) return false;
        return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => obj is Enumeration<TEnum> other && Equals(other);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    public static bool operator ==(Enumeration<TEnum>? left, Enumeration<TEnum>? right)
        => Equals(left, right);

    public static bool operator !=(Enumeration<TEnum>? left, Enumeration<TEnum>? right)
        => !Equals(left, right);
}