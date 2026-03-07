namespace OIO.Infrastructure.Settings;

public sealed class DefaultAccountOptions
{
    public const string SectionName = "DefaultAccount";
    public string Email { get; init; } = null!;
    public string UserName { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string DisplayName { get; init; } = null!;

}