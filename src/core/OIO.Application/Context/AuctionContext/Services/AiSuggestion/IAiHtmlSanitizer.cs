namespace OIO.Application.Context.AuctionContext.Services.AiSuggestion;

/// <summary>
/// Sanitizes AI-generated description fragments into a safe HTML allow-list
/// suitable for the FE Quill RichTextEditor. Backend is the source of truth
/// for HTML safety; the FE is a defense-in-depth layer.
///
/// Allowed tags: p, br, strong, em, ul, ol, li. All attributes are stripped.
/// </summary>
public interface IAiHtmlSanitizer
{
    SanitizedHtml Sanitize(string? raw);

    /// <summary>
    /// Truncates already-sanitized HTML so the rendered plain-text length
    /// is at most <paramref name="maxPlainChars"/>. Implementation may re-emit
    /// as a single paragraph to preserve safety after truncation.
    /// </summary>
    string TruncateToBudget(string sanitizedHtml, int maxPlainChars);
}

public sealed record SanitizedHtml(string Html, string PlainText);
