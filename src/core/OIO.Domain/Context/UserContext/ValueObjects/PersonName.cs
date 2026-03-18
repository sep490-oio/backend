using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.ValueObjects;

public sealed class PersonName : ValueObject
{
    public string? FirstName { get; }
    public string? LastName { get; }
    public string? DisplayName { get; }

    private PersonName(string? firstName, string? lastName, string? displayName)
    {
        FirstName = firstName;
        LastName = lastName;
        DisplayName = displayName;
    }

    public static PersonName Create(
        string? firstName = null, 
        string? lastName = null, 
        string? displayName = null)
        => new(firstName?.Trim(), lastName?.Trim(), displayName?.Trim());

    public string FullName => $"{FirstName} {LastName}".Trim();

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return FirstName ?? string.Empty;
        yield return LastName ?? string.Empty;
        yield return DisplayName ?? string.Empty;
    }
}