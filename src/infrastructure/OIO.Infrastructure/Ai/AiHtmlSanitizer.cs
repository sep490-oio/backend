using System.Net;
using System.Text.RegularExpressions;
using Ganss.Xss;
using OIO.Application.Context.AuctionContext.Services.AiSuggestion;

namespace OIO.Infrastructure.Ai;

/// <summary>
/// Sanitizes AI-generated description fragments into a strict HTML allow-list
/// that the FE Quill RichTextEditor can safely render.
///
/// Backend is the source of truth for safety; the FE is defense-in-depth.
/// Allowed tags: p, br, strong, em, ul, ol, li.
/// All attributes (style, class, id, href, src, on*) are stripped.
/// </summary>
internal sealed class AiHtmlSanitizer : IAiHtmlSanitizer
{
    private static readonly HtmlSanitizer Sanitizer = CreateSanitizer();
    private static readonly Regex TagRegex = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex MultipleBreaks = new("(<br\\s*/?>\\s*){3,}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MultipleWhitespace = new("[ \\t\\n\\r]{2,}", RegexOptions.Compiled);

    public SanitizedHtml Sanitize(string? raw)
    {
        var input = (raw ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            return new SanitizedHtml(string.Empty, string.Empty);
        }

        // If the input has no recognizable HTML tag near the start, treat as plain text.
        var html = LooksLikeHtml(input)
            ? Sanitizer.Sanitize(input)
            : EncodeAsParagraph(input);

        // Collapse runs of <br> down to at most 2 (paragraph break).
        html = MultipleBreaks.Replace(html, "<br /><br />");

        // Compute plain text after sanitation for length budgeting.
        var plain = HtmlToPlainText(html);

        // If sanitation removed everything (e.g. only forbidden tags), return empty.
        if (string.IsNullOrWhiteSpace(plain))
        {
            return new SanitizedHtml(string.Empty, string.Empty);
        }

        return new SanitizedHtml(html, plain);
    }

    public string TruncateToBudget(string sanitizedHtml, int maxPlainChars)
    {
        var plain = HtmlToPlainText(sanitizedHtml);
        if (plain.Length <= maxPlainChars)
        {
            return sanitizedHtml;
        }

        // Re-emit as a single paragraph of truncated plain text, HTML-encoded.
        // This keeps the safety guarantee even when truncation breaks tag balance.
        var truncated = plain[..maxPlainChars].TrimEnd();
        return EncodeAsParagraph(truncated);
    }

    private static string HtmlToPlainText(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        var stripped = TagRegex.Replace(html, " ");
        var decoded = WebUtility.HtmlDecode(stripped);
        return MultipleWhitespace.Replace(decoded, " ").Trim();
    }

    private static bool LooksLikeHtml(string input)
    {
        // Heuristic: a '<' followed by an ASCII letter within the first 200 chars.
        var probe = input.Length > 200 ? input[..200] : input;
        for (var i = 0; i < probe.Length - 1; i++)
        {
            if (probe[i] == '<' && char.IsLetter(probe[i + 1]))
            {
                return true;
            }
        }
        return false;
    }

    private static string EncodeAsParagraph(string plainText)
    {
        var encoded = WebUtility.HtmlEncode(plainText);
        // Convert blank-line breaks into paragraph breaks; single newlines into <br>.
        encoded = encoded.Replace("\r\n", "\n");
        var paragraphs = encoded
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(p => "<p>" + p.Replace("\n", "<br />") + "</p>");
        var joined = string.Concat(paragraphs);
        return string.IsNullOrEmpty(joined) ? string.Empty : joined;
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var s = new HtmlSanitizer();
        s.AllowedTags.Clear();
        s.AllowedTags.UnionWith(new[] { "p", "br", "strong", "em", "ul", "ol", "li" });

        s.AllowedAttributes.Clear();
        s.AllowedSchemes.Clear();
        s.AllowedCssProperties.Clear();
        s.AllowedAtRules.Clear();

        // Drop unsafe nodes; keep child text where applicable.
        s.KeepChildNodes = true;
        return s;
    }
}
