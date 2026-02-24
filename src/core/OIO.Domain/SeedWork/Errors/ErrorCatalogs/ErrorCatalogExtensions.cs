namespace OIO.Domain.SeedWork.Errors.ErrorCatalogs;

public static class ErrorCatalogExtensions
{
    extension(OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog)
    {
        public static string GetDefaultTemplate(string rule) => rule switch
        {
            // Required
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Required.NotNull => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Required.NotNull,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Required.NotEmpty => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Required.NotEmpty,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Required.NotEmptyGuid => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Required.NotEmptyGuid,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Required.NotDefault => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Required.NotDefault,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Required.NotBlank => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Required.NotBlank,

            // Text
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Text.Matches => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Text.Matches,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Text.StartsWith => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Text.StartsWith,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Text.EndsWith => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Text.EndsWith,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Text.Contains => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Text.Contains,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Text.MaxLength => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Text.MaxLength,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Text.MinLength => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Text.MinLength,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Text.ExactLength => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Text.ExactLength,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Text.LengthBetween => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Text.LengthBetween,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Text.Format => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Text.Format,

            // Numeric
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Numeric.Positive => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Numeric.Positive,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Numeric.Negative => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Numeric.Negative,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Numeric.NonPositive => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Numeric.NonPositive,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Numeric.NonNegative => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Numeric.NonNegative,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Numeric.Zero => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Numeric.Zero,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Numeric.NonZero => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Numeric.NonZero,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Numeric.MultipleOf => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Numeric.MultipleOf,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Numeric.PrecisionScale => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Numeric.PrecisionScale,

            // Collection
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Collection.NotContains => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Collection.NotContains,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Collection.NotInSet => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Collection.NotInSet,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Collection.InSet => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Collection.InSet,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Collection.NoDuplicates => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Collection.NoDuplicates,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Collection.CountMin => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Collection.CountMin,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Collection.CountMax => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Collection.CountMax,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Collection.CountBetween => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Collection.CountBetween,

            // Compare
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Compare.Equal => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Compare.Equal,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Compare.NotEqual => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Compare.NotEqual,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Compare.BetweenInclusive => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Compare.BetweenInclusive,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Compare.BetweenExclusive => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Compare.BetweenExclusive,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Compare.GreaterThan => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Compare.GreaterThan,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Compare.GreaterThanOrEqual => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Compare.GreaterThanOrEqual,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Compare.LessThan => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Compare.LessThan,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Compare.LessThanOrEqual => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Compare.LessThanOrEqual,

            // Date
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Time.After => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Time.After,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Time.Before => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Time.Before,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Time.TimeRange => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Time.TimeRange,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Time.NotInPast => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Time.NotInPast,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Time.NotInFuture => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Time.NotInFuture,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Time.Utc => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Time.Utc,
            OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.Rule.Time.NotOverlapping => OIO.Domain.SeedWork.Errors.ErrorCatalogs.ErrorCatalog.DefaultTemplate.Time.NotOverlapping,

            _ => "{field} is invalid."
        };
    }
}