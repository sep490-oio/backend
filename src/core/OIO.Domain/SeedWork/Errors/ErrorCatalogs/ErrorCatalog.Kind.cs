namespace OIO.Domain.SeedWork.Errors.ErrorCatalogs;

public static partial class ErrorCatalog
{
    public static partial class Kind
    {
        public const string Conflict = "Conflict"; //409
        public const string Validation = "Validation"; //400
        public const string Unauthorized = "Unauthorized"; //401
        public const string Forbidden = "Forbidden"; //403
        public const string NotFound = "NotFound"; //404
        public const string Invariant = "Invariant"; //422
        public const string Violations = "Violations"; //422 combine error of Invariant and Validation
        public const string Unexpected = "Unexpected"; //500
        public const string Unavailable = "Unavailable"; //503
        public const string Timeout = "Timeout"; //504
    }
}