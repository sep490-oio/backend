namespace OIO.Domain.AppDefinitions;

public static partial class App
{
    public static class Constraint
    {
        public static class Address
        {
            public const int StreetMaxLength = 255;
            public const int WardMaxLength = 100;
            public const int DistrictMaxLength = 100;
            public const int CityMaxLength = 120;
            public const int PostalCodeMaxLenght = 10;
        }

        public static class AvatarUrl
        {
            public const int MinLength = 5;
            public const int MaxLength = 255;
        }

        public static class DisplayName
        {
            public const int MinLength = 1;
            public const int MaxLength = 100;
        }

        public static class UserEmail
        {
            public const int MaxLength = 255;
            public const string Regex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        }

        public static class FirstName
        {
            public const int MinLength = 1;
            public const int MaxLength = 50;
        }
    
        public static class LastName
        {
            public const int MinLength = 1;
            public const int MaxLength = 50;
        }

        public static class Password
        {
            public const int MinLength = 8;
            public const int MaxLength = 128;

            public const string FormatMessage =
                "Password must have one uppercase letter, one lowercase letter, one digit, and one special character";
            public static readonly Func<string, bool>[] Validator =
            [
                s => s.Any(char.IsUpper),
                s => s.Any(char.IsUpper),
                s => s.Any(char.IsLower),
                s => s.Any(char.IsDigit),
                s => s.Any(ch => !char.IsLetterOrDigit(ch))
            ];
        }

        public static class UserName
        {
            public const int MinLength = 3;
            public const int MaxLength = 50;
            public const string Regex = "^[a-zA-Z0-9_-]+$";
        }

        public static class UserAddress
        {
            public const int RecipientNameMaxLength = 100;
        }

        public static class Item
        {
            public const int TitleMaxLength = 255;
        }
    }
}