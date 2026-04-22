using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.Enums;

public sealed class TermsDocumentStatus : EnumValueObject<TermsDocumentStatus>
{
    public static readonly TermsDocumentStatus Draft = new("draft");
    public static readonly TermsDocumentStatus Active = new("active");
    public static readonly TermsDocumentStatus Archived = new("archived");

    private TermsDocumentStatus(string id) : base(id) { }

    /// <summary>
    /// Enforces the legal 3-state lifecycle transition matrix per ralplan §3.1.
    /// Allowed: Draft → Active (via Activate), Active → Archived (via Archive or auto on sibling activation).
    /// Forbidden: Active → Draft, Archived → any, Draft → Archived (direct).
    /// </summary>
    public bool CanTransitionTo(TermsDocumentStatus target) =>
        (Id, target.Id) switch
        {
            ("draft", "active") => true,
            ("active", "archived") => true,
            _ => false
        };
}
