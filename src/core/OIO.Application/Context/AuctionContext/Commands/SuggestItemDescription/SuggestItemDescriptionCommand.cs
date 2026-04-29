using OIO.Application.Abstractions.Messaging;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.SuggestItemDescription;

public sealed record SuggestItemDescriptionCommand(
    string Title,
    string Condition,
    IReadOnlyList<Guid> ImageMediaUploadIds,
    string? Locale = "vi") : ICommand<SuggestItemDescriptionResult>, IHasValidate
{
    public ViolationsError Validate()
    {
        var check = SuggestItemDescriptionCommand
            .Check()
            .WithOwnerName("SuggestItemDescription")
            .Field(Title)
            .NotWhiteSpace()
            .MaxLength(App.Constraint.Item.TitleMaxLength)
            .Field(Condition)
            .NotWhiteSpace()
            .InSet(ItemCondition.All.Select(x => x.Id))
            .ToViolationsError();

        if (ImageMediaUploadIds is null || ImageMediaUploadIds.Count == 0)
        {
            check.Add(Error.Validation(
                "ImageMediaUploadIds",
                "ImageMediaUploadIds.OutOfRange",
                "At least one image media upload id is required."));
        }
        else
        {
            // Hard upper bound to guard against absurd payloads. The actual
            // per-request cap is Ai:ProductDescription:MaxImages and is enforced
            // by the handler against the configured option (default 3).
            if (ImageMediaUploadIds.Count > 10)
            {
                check.Add(Error.Validation(
                    "ImageMediaUploadIds",
                    "ImageMediaUploadIds.OutOfRange",
                    "Too many image media upload ids; maximum is 10."));
            }

            for (var i = 0; i < ImageMediaUploadIds.Count; i++)
            {
                if (ImageMediaUploadIds[i] == Guid.Empty)
                {
                    check.Add(Error.Validation(
                        $"ImageMediaUploadIds[{i}]",
                        "ImageMediaUploadIds.NotEmpty",
                        "Image media upload id must not be empty."));
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(Locale)
            && !string.Equals(Locale, "vi", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(Locale, "en", StringComparison.OrdinalIgnoreCase))
        {
            check.Add(Error.Validation(
                "Locale",
                "Locale.Unsupported",
                "Locale must be either 'vi' or 'en'."));
        }

        return check;
    }
}
