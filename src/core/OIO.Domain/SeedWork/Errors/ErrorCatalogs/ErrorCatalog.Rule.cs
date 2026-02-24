namespace OIO.Domain.SeedWork.Errors.ErrorCatalogs;

public static partial class ErrorCatalog
{
    public static partial class Rule
    {
        public static partial class Required
        {
            public const string NotNull = "NotNull";
            public const string NotEmpty = "NotEmpty";
            public const string NotEmptyGuid = "NotEmptyGuid";
            public const string NotDefault = "NotDefault";
            public const string NotBlank = "NotBlank"; 
        }

        public static partial class Text
        {
            public const string Matches = "Matches";
            public const string StartsWith = "StartsWith";
            public const string EndsWith = "EndsWith";
            public const string Contains = "Contains";
            public const string MaxLength = "MaxLength";
            public const string MinLength = "MinLength";
            public const string ExactLength = "ExactLength";
            public const string LengthBetween = "LengthBetween";
            public const string Format = "Format";
        }

        public static partial class Numeric
        {
            public const string Positive = "Positive"; // > 0
            public const string Negative = "Negative"; // < 0
            public const string NonPositive = "NonPositive"; // <= 0
            public const string NonNegative = "NonNegative"; // >= 0
            public const string Zero = "Zero"; // == 0
            public const string NonZero = "NonZero"; // != 0
            public const string MultipleOf = "MultipleOf";
            public const string PrecisionScale = "PrecisionScale";
            public const string Maximum = "Maximum";
            public const string Minimum = "Minimum";
        }

        public static partial class Collection
        {
            public const string NotContains =  "NotContains";
            public const string NotInSet = "NotInSet";
            public const string InSet = "InSet";
            public const string NoDuplicates = "NoDuplicates";
            public const string CountMin = "CountMin";
            public const string CountMax = "CountMax";
            public const string CountBetween = "CountBetween";
        }

        public static partial class Scalar
        {
            public const string InSet = "InSet";
            public const string NotInSet = "NotInSet";
        }

        public static partial class Enum
        {
            public const string InEnum = "InEnum";
        }

        public static partial class Compare
        {
            public const string Equal = "Equal";
            public const string NotEqual = "NotEqual";
            public const string BetweenInclusive = "BetweenInclusive";
            public const string BetweenExclusive = "BetweenExclusive";
            public const string GreaterThan = "GreaterThan";
            public const string GreaterThanOrEqual = "GreaterThanOrEqual";
            public const string LessThan = "LessThan";
            public const string LessThanOrEqual = "LessThanOrEqual"; 
        }

        public static partial class Time
        {
            public const string After = "After";
            public const string Before = "Before";
            public const string TimeRange = "TimeRange";
            public const string NotInPast = "NotInPast";
            public const string NotInFuture = "NotInFuture";
            public const string Utc = "Utc";
            public const string NotOverlapping = "NotOverlapping";
        }
    }
}