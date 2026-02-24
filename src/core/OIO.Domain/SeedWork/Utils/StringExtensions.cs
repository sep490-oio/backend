using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace OIO.Domain.SeedWork.Utils;


public static partial class StringExtensions
{
    public static readonly ConcurrentDictionary<string, string> NameCache = new();
    
    extension(string text)
    {
        public string ToSnakeCase()
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            var result = SnakeCaseRegex().Replace(text, "_");
        
            // Convert the entire string to lowercase
            return result.ToUpperInvariant();
        }

        public string ExtractLastMember()
        {
            if (string.IsNullOrWhiteSpace(text)) return "Value";

            var lastDot = text.LastIndexOf('.');
            var token = lastDot >= 0 ? text[(lastDot + 1)..] : text;
            token = token.Trim();

            // bỏ call "Trim()" -> "Trim"
            var paren = token.IndexOf('(');
            if (paren >= 0) token = token[..paren];
            
            paren = token.LastIndexOf(')');
            if(paren >= 0) token = token[..paren];

            // bỏ indexer "Items[0]" -> "Items"
            var bracket = token.IndexOf('[');
            if (bracket >= 0) token = token[..bracket];

            return (string.IsNullOrWhiteSpace(token) ? "Value" : token);
        }

        public string Format(IReadOnlyDictionary<string, object?> args)
        {
            if (string.IsNullOrEmpty(text) || args.Count == 0) return text;

            return FormatRegex().Replace(text, m =>
            {
                var key = m.Groups[1].Value;
                return args.TryGetValue(key, out var v) ? (v?.ToString() ?? "") : m.Value;
            });
        }

        public string ToTitleCase()
        {
            if (string.IsNullOrWhiteSpace(text)) return "Value";
            
            text = text.ToLower().Trim();
    
            var textInfo = CultureInfo.CurrentCulture.TextInfo;
    
            return textInfo.ToTitleCase(text);
        }
    }

    extension<TOwner, TProperty>(Expression<Func<TOwner, TProperty>> expr)
    {
        public string ExtractMemberPath()
        {
            var body = expr.Body;

            if (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u)
                body = u.Operand;

            var parts = new List<string>(4);

            while (body is MemberExpression m)
            {
                parts.Add(m.Member.Name);
                body = m.Expression!;
            }

            if (body is not ParameterExpression)
                throw new ArgumentException("Expression must be a simple member access off the parameter, e.g. x => x.A.B", nameof(expr));

            parts.Reverse();
            return string.Join(".", parts);
        }
        
        public string GetOrAddName()
        {
            var path = expr.ExtractMemberPath();                
            var key  = $"{typeof(TOwner).FullName}:{path}";    

            return NameCache.GetOrAdd(key, _ => path);
        }

    }

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex FormatRegex();
    
    [GeneratedRegex(@"(?<=[a-z0-9])(?=[A-Z])")]
    private static partial Regex SnakeCaseRegex();
}
