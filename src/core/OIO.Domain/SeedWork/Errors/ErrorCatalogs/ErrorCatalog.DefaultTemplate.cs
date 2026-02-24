namespace OIO.Domain.SeedWork.Errors.ErrorCatalogs;

public static partial class ErrorCatalog
{
    public static partial class DefaultTemplate
    {
        public static partial class Required
        {
            public const string NotNull = "{field} must not be null.";
            public const string NotEmpty = "{field} must not be empty.";
            public const string NotEmptyGuid = "{field} must not be an empty GUID.";
            public const string NotDefault = "{field} must not be the {default} value.";
            public const string NotBlank = "{field} must not be blank."; 
        }

        public static partial class Text
        {
            public const string Matches = "{field} must match the required pattern. Pattern: {pattern}";
            public const string StartsWith = "{field} must start with \"{prefix}\".";
            public const string EndsWith = "{field} must end with \"{suffix}\".";
            public const string Contains = "{field} must contain \"{substring}\".";
            public const string MaxLength = "{field} must be at most {max} characters long.";
            public const string MinLength = "{field} must be at least {min} characters long.";
            public const string ExactLength = "{field} must be exactly {length} characters long.";
            public const string LengthBetween = "{field} must be between {min} and {max} characters long.";
            public const string Format = "{field} has an invalid format.";
        }

        public static partial class Numeric
        {
            public const string Positive = "{field} must be greater than 0."; // > 0
            public const string Negative = "{field} must be less than 0."; // < 0
            public const string NonPositive = "{field} must be less than or equal to 0."; // <= 0
            public const string NonNegative = "{field} must be greater than or equal to 0."; // >= 0
            public const string Zero = "{field} must be 0."; // == 0
            public const string NonZero = "{field} must not be 0."; // != 0
            public const string MultipleOf = "{field} must be a multiple of {step}.";
            public const string PrecisionScale = "{field} must have at most {precision} digits in total, with up to {scale} decimal places.";
        }

        public static partial class Collection
        {
            public const string NotContains =  "{field} must not contain \"{item}\".";
            public const string NotInSet = "{field} must not be one of: {set}.";
            public const string InSet = "{field} must be one of: {set}.";
            public const string NoDuplicates = "{field} must not contain duplicate values. Duplicate: {duplicate}";
            public const string CountMin = "{field} must contain at least {min} item(s).";
            public const string CountMax = "{field} must contain at most {max} item(s).";
            public const string CountBetween = "{field} must contain between {min} and {max} item(s).";
        }
        
        public static partial class Scalar
        {
            public const string InSet = "{field} must be one of: {set}.";
            public const string NotInSet = "{field} must not be one of: {set}.";
        }

        public static partial class Enum
        {
            public const string InEnum = "{field} must be a defined value of enum {enum}. Value: {value}.";
        }

        public static partial class Compare
        {
            public const string Equal = "{field} must be equal to {value}.";
            public const string NotEqual = "{field} must not be equal to {value}.";
            public const string BetweenInclusive = "{field} must be between {min} and {max} (inclusive).";
            public const string BetweenExclusive = "{field} must be between {min} and {max} (exclusive).";
            public const string GreaterThan = "{field} must be greater than {value}.";
            public const string GreaterThanOrEqual = "{field} must be greater than or equal to {value}.";
            public const string LessThan = "{field} must be less than {value}.";
            public const string LessThanOrEqual = "{field} must be less than or equal to {value}."; 
        }

        public static partial class Time
        {
            public const string After = "{field} must be after {time}.";
            public const string Before = "{field} must be before {time}.";
            public const string TimeRange = "{field} must be between {start} and {end}.";
            public const string NotInPast = "{field} must not be in the past.";
            public const string NotInFuture = "{field} must not be in the future.";
            public const string Utc = "{field} must be in UTC.";
            public const string NotOverlapping = "{field} must not overlap with an existing range.";
        }
    }
}