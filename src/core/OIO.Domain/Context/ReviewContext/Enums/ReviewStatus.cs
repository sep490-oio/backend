using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ReviewContext.Enums;

public sealed class ReviewStatus : EnumValueObject<ReviewStatus>
{
    public static readonly ReviewStatus Pending = new("pending");
    public static readonly ReviewStatus Published = new("published");
    public static readonly ReviewStatus Hidden = new("hidden");
    public static readonly ReviewStatus Removed = new("removed");
    private ReviewStatus(string id) : base(id) { }
}